using SE_lab.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SE_lab
{
    public class Problem
    {
        public ulong Id { set; get; }
        public byte[] Data { set; get; }
        public string ProblemType { set; get; }
        public ulong solvingTimeout { set; get; }
        public bool timeoutSpecified { set; get; }
        public bool timeoutOccured { set; get; }
        public SolutionsSolution[] solutions;
        public SolvePartialProblemsPartialProblem[] problems;
        public ComputationalNode[] nodes;
        public TaskManager manager;
        public Problem()
        {
            //solutions = new List<SolutionsSolution>();
            // problems = new List<SolvePartialProblemsPartialProblem>();
            // nodes = new List<ComputationalNode>();
        }
    }
}
