using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UCCTaskSolver;

namespace DVRPResolver
{
    public partial class DVRPTaskSolver:TaskSolver
    {
        private DVRPDescription description;
        private SolutionDescription solution;
        private double[,] distances;
        private Timer timer;

        public DVRPTaskSolver(byte[] data)
            : base(data)
        {
            if (data.Length != 0)
            {
                description = SerializeToClass<DVRPDescription>(data);
                State = TaskSolverState.Idle;
            }
        }

        public override byte[][] DivideProblem(int threadCount)
        {
            State = TaskSolverState.Dividing;
            List<List<List<int>>> startingDivisions = new List<List<List<int>>>();
            var allDivisions = new List<List<List<int>>>() { new List<List<int>>() { new List<int>() { 0 } } };
            for (int i = 1; i < description.clients.Count; i++)
            {
                foreach (var division in allDivisions)
                {
                    for (int j = 0; j < division.Count; j++)
                    {
                        List<List<int>> newDivision = new List<List<int>>();
                        foreach (var div in division)
                            newDivision.Add(new List<int>(div));
                        newDivision[j].Add(i);
                        startingDivisions.Add(newDivision);
                    }
                    List<List<int>> newDivision2 = new List<List<int>>();
                    foreach (var div in division)
                        newDivision2.Add(new List<int>(div));
                    newDivision2.Add(new List<int>() { i });
                    startingDivisions.Add(newDivision2);
                }
                allDivisions = new List<List<List<int>>>(startingDivisions);
                startingDivisions.Clear();
                if (allDivisions.Count > threadCount) break;
            }
            allDivisions.RemoveAll(division => division.Any(subset => subset.Sum(client => description.clients[client].demand) > description.vehicleCapacity));
            PartialProblems = new byte[allDivisions.Count][];
            for (int i = 0; i < PartialProblems.Length; i++)
                PartialProblems[i] = SerializeFromClass<List<List<int>>>(allDivisions[i]);
           // ProblemDividingFinished.Invoke(EventArgs.Empty, this);
            State = TaskSolverState.Idle;
            return PartialProblems;
        }

        public override void MergeSolution(byte[][] solutions)
        {
            State = TaskSolverState.Merging;
            SolutionDescription bestSolution = SerializeToClass<SolutionDescription>(solutions[0]);
            for (int i = 1; i < solutions.Length; i++)
            {
                SolutionDescription nextSolution = SerializeToClass<SolutionDescription>(solutions[i]);
                if (bestSolution.m_result > nextSolution.m_result) bestSolution = nextSolution;
            }
            Solution = SerializeFromClass<SolutionDescription>(bestSolution);
            State = TaskSolverState.Idle;
            SolutionsMergingFinished.Invoke(EventArgs.Empty, this);
        }

        public override string Name
        {
            get { return "DVRP-02"; }
        }

        public override event TaskSolver.ComputationsFinishedEventHandler ProblemDividingFinished;

        public override event TaskSolver.ComputationsFinishedEventHandler ProblemSolvingFinished;

        public override event TaskSolver.ComputationsFinishedEventHandler SolutionsMergingFinished;

        private int[][] actualDivision;
        private int[] actualCosts;
        private int[] subsetsCount;
        private int N;

        public override byte[] Solve(byte[] partialData, TimeSpan timeout)
        {
            if (timeout != TimeSpan.Zero)
            {
                timer = new Timer(timeout.TotalMilliseconds);
                timer.Elapsed += TimeoutOcurred;
                timer.Start();
            }
            solution = new SolutionDescription() { m_result = double.MaxValue,m_permutation=new int[0][]};
            State = TaskSolverState.Solving;
            var startingDivision = SerializeToClass<List<List<int>>>(partialData);
            PrepareData();
            N = description.clients.Count;
            actualDivision = new int[N][];
            for (int i = 0; i < actualDivision.Length; i++)
                actualDivision[i] = new int[N];
            actualCosts = new int[N];
            subsetsCount = new int[N];
            int nextNr = 0;
            for (int i = 0; i < startingDivision.Count; i++)
            {
                subsetsCount[i] = startingDivision[i].Count;
                for (int j = 0; j < startingDivision[i].Count; j++)
                {
                    actualDivision[i][j] = startingDivision[i][j];
                    actualCosts[i] += description.clients[startingDivision[i][j]].demand;
                    nextNr++;
                }
            }
            MakeAllDivisions(nextNr, startingDivision.Count);
            Solution = SerializeFromClass<SolutionDescription>(solution);
          //  ProblemSolvingFinished.Invoke(EventArgs.Empty, this);
            State = TaskSolverState.Idle;
            return Solution;
        }

        void TimeoutOcurred(object sender, ElapsedEventArgs e)
        {
            State = TaskSolverState.Timeout;
            timer.Stop();
        }

        private T SerializeToClass<T>(byte[] classInBytes)
        {
            MemoryStream ms = new MemoryStream(classInBytes);
            BinaryFormatter bf = new BinaryFormatter();
            ms.Write(classInBytes, 0, classInBytes.Length);
            ms.Seek(0, SeekOrigin.Begin);
            Object o = bf.Deserialize(ms);
            return (T)o;
        }

        private byte[] SerializeFromClass<T>(T obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream();
            bf.Serialize(ms, obj);
            return ms.ToArray();
        }

        public override event UnhandledExceptionEventHandler ErrorOccured;
    }
}
