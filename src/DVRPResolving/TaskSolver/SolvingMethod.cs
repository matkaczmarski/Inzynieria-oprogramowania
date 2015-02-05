using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using UCCTaskSolver;

namespace DVRPResolver
{
    public partial class DVRPTaskSolver : TaskSolver
    {
        public void PrepareData()
        {
            distances = new double[description.clients.Count + 1, description.clients.Count + 1];
            for (int i = 0; i < description.clients.Count; i++)
                distances[0, i + 1] = distances[i + 1, 0] =
                    description.CountDistance(description.coordinateDepot, description.clients[i].coordinate);
            for (int i = 0; i < description.clients.Count; i++)
                for (int j = i + 1; j < description.clients.Count; j++)
                    distances[i + 1, j + 1] = distances[j + 1, i + 1] =
                        description.CountDistance(description.clients[i].coordinate, description.clients[j].coordinate);
            for (int i = 0; i < description.clients.Count; i++)
                if (description.clients[i].availableTime > description.cutOffTime * description.endTimeDepot)
                    description.clients[i].availableTime = 0;
        }

        private void MakeAllDivisions(int index, int setsNr)
        {
            if (State == TaskSolverState.Timeout) return;
            if (index == N)
            {
                ResolveOneDivision(actualDivision, setsNr);
                return;
            }
            for (int i = 0; i < setsNr; i++)
            {
                actualCosts[i] += description.clients[index].demand;
                if (actualCosts[i] <= description.vehicleCapacity)
                {
                    actualDivision[i][subsetsCount[i]++] = index;
                    MakeAllDivisions(index + 1, setsNr);
                    subsetsCount[i]--;
                }
                actualCosts[i] -= description.clients[index].demand;
            }
            actualCosts[setsNr] += description.clients[index].demand;
            actualDivision[setsNr][subsetsCount[setsNr]++] = index;
            MakeAllDivisions(index + 1, setsNr + 1);
            subsetsCount[setsNr]--;
            actualCosts[setsNr] -= description.clients[index].demand;
        }

        private void ResolveOneDivision(int[][] oneDivision, int setsNr)
        {
            if (State == TaskSolverState.Timeout) return;
            /*double downPredictionsSum = 0;
            double[] downPredictions = new double[setsNr];
            for (int i = 0; i < setsNr; i++)
            {
                downPredictions[i] = KruskalAlgorithm(oneDivision[i], subsetsCount[i]);
                if (downPredictions[i] == double.MaxValue) return;
                downPredictionsSum += oneSubsetMinCost;
                if (downPredictionsSum > solution.m_result) return;
            }*/
            double oneDivisionMinCost = 0;
            int[][] oneDivisionMinPermutations = new int[setsNr][];
            for (int i = 0; i < setsNr; i++)
            {
                //double oneSubsetMinCost = double.MaxValue;// 2 * downPredictions[i];
                double oneSubsetMinCost = /*subsetsCount[i] < 5 ?*/ AllPermuationsSimple(oneDivision[i], subsetsCount[i]);/* :*/
                //  AllPermutationsComplicated(oneDivision[i], subsetsCount[i], out oneSubsetMinPermutation);
                if (oneSubsetMinCost == double.MaxValue)
                    return;
                oneDivisionMinCost += oneSubsetMinCost;
                oneDivisionMinPermutations[i] = oneSubsetMinPermutation;
                if (oneDivisionMinCost > solution.m_result)
                    return;
            }
            if (oneDivisionMinPermutations.Length > description.vehiclesCount)
            {
                List<List<int>> oneDivisionMinPermutationList;
                List<List<int>> oneDiv = new List<List<int>>();
                for (int i = 0; i < setsNr; i++)
                {
                    List<int> subset = new List<int>();
                    for (int j = 0; j < subsetsCount[i]; i++)
                        subset.Add(oneDivision[i][j]);
                    oneDiv.Add(subset);
                }
                double value = LittleCarProblem0(oneDiv, out oneDivisionMinPermutationList);
                if (value >= solution.m_result) return;
                oneDivisionMinPermutations = new int[oneDivisionMinPermutationList.Count][];
                for (int i = 0; i < oneDivisionMinPermutations.Length; i++)
                {
                    oneDivisionMinPermutations[i] = new int[oneDivisionMinPermutationList[i].Count];
                    for (int j = 0; j < oneDivisionMinPermutations[i].Length; j++)
                        oneDivisionMinPermutations[i][j] = oneDivisionMinPermutationList[i][j];
                }
            }
            solution = new SolutionDescription() { m_permutation = oneDivisionMinPermutations, m_result = oneDivisionMinCost };
        }

        /*
        public class Edge
        {
            public int from;public int to;public double weight;
            public Edge(int _from, int _to, double _weight) { from = _from; to = _to; weight = _weight; }
        }

        private double KruskalAlgorithm(int[] clients, int clientCount)
        {
            List<Edge> queue = new List<Edge>();
            var partsIndex = new int[clientCount + 1];
            for (int i = 1; i < partsIndex.Length; i++)
                partsIndex[i] = i;
            for (int i = 0; i < clientCount; i++)
                queue.Add(new Edge(0, i + 1, distances[0, clients[i] + 1]));
            for (int i = 0; i < clientCount - 1; i++)
                for (int j = i + 1; j < clientCount; j++)
                    queue.Add(new Edge(i + 1, j + 1, distances[clients[i] + 1, clients[j] + 1]));
            queue.Sort((e1, e2) => { return Math.Sign(-e1.weight + e2.weight); });
            Stack<Edge> priority = new Stack<Edge>(queue);
            int edgesCount = 0;
            double cost = 0;
            while (edgesCount != clientCount)
            {
                Edge e = priority.Pop();
                if (partsIndex[e.from] != partsIndex[e.to])
                {   int ind=partsIndex[e.to];
                    for (int i = 0; i < partsIndex.Length; i++)
                        if (partsIndex[i] == ind)
                            partsIndex[i] = partsIndex[e.from];
                    cost += e.weight;
                    edgesCount++;
                }
            }
            if (cost + clients.Take(clientCount).Sum(index => description.clients[index].durationTime) > description.endTimeDepot - description.startTimeDepot)
                return double.MaxValue;
            return 2 * cost;
        }
        */

        private double AllPermuationsSimple(int[] clientsNumbers, int clientsCount)
        {
            Nk = clientsCount;
            used = new bool[Nk];
            actualPermutation = new int[Nk];
            actualSubset = clientsNumbers;
            oneSubsetMinPermutation = new int[Nk];
            oneSubsetMinCost = double.MaxValue;
            FindBestPermutation(0, description.startTimeDepot, 0, 0);
            return oneSubsetMinCost;
        }

        private double oneSubsetMinCost;
        private int[] oneSubsetMinPermutation;

        private int[] actualSubset;
        private int[] actualPermutation;
        private bool[] used;
        private int Nk;

        private void FindBestPermutation(int position, double timeWindow, double cost,int lastPoint)
        {
            if (State == TaskSolverState.Timeout || timeWindow > description.endTimeDepot || oneSubsetMinCost <= cost) return;
            if (position == Nk)
            {
                cost += distances[lastPoint, 0];
                timeWindow += distances[lastPoint, 0];
                if (timeWindow <= description.endTimeDepot && oneSubsetMinCost > cost)
                {
                    oneSubsetMinCost = cost;
                    Array.Copy(actualPermutation, oneSubsetMinPermutation, actualPermutation.Length);
                   // oneSubsetMinPermutation = (int[])actualPermutation.Clone();
                }
                return;
            }
            for (int i = 0; i < Nk; i++)
            {
                if (!used[i])
                {
                    used[i] = true;
                    actualPermutation[position] = actualSubset[i];
                    double newTimeWindow = timeWindow;
                    if (description.clients[actualSubset[i]].availableTime > newTimeWindow)
                        newTimeWindow = description.clients[actualSubset[i]].availableTime;
                    FindBestPermutation(position + 1,
                        newTimeWindow + distances[actualPermutation[position] + 1, lastPoint] + description.clients[actualPermutation[position]].durationTime,
                        cost + distances[actualPermutation[position] + 1, lastPoint], actualPermutation[position] + 1);
                    used[i] = false;
                }
            }
        }

        //private int[] ResolveAllPermutations(int[] first)
        //{
        //    bool even = true;
        //    int[] positions = (int[])first.Clone();
        //    int maximum = -1;
        //    bool maxEven = false;
        //    for (int i = 0; i < positions.Length; i++)
        //    {
        //        if (even)
        //        {
        //            if (positions[i] - 1 >= 0 && first[positions[i] - 1] < i)
        //            { maximum = i; maxEven = true; }
        //        }
        //        else
        //        {
        //            if (positions[i] + 1 < positions.Length && first[positions[i] + 1] < i)
        //            { maximum = i; maxEven = false; }
        //        }
        //    }
        //    if (maxEven)
        //    {
        //        int c = first[positions[maximum]]; first[positions[maximum]] = first[positions[maximum - 1]];
        //        first[positions[maximum - 1]] = c;
        //    }
        //    else
        //    {
        //        int c = first[positions[maximum]]; first[positions[maximum]] = first[positions[maximum + 1]];
        //        first[positions[maximum + 1]] = c;
        //    }
        //    return first;
        //}

        //public double AllPermutationsComplicated(int[] clientsNr, int clientsCount, out int[] minPermutation)
        //{
        //    bool isLegalResult;
        //    minPermutation = null;
        //    double minCost = double.MaxValue;
        //    double actualCost = CountCost(clientsNr, clientsCount, out isLegalResult);
        //    int[] actualPermutation = new int[clientsCount];
        //   // int[] actualPosition = new int[clientsCount];
        //    for (int i = 0; i < actualPermutation.Length; i++)
        //    { actualPermutation[i] = -(i + 1); /*actualPosition[i] = i;*/ }
        //    if (isLegalResult)
        //    {
        //        minPermutation = (int[])actualPermutation.Clone();
        //        minCost = actualCost;
        //    }
        //    int k, lastMax, kIndex;
        //    while (true)
        //    {/*
        //        for (k = actualPosition.Length - 1; k >= 0; k--)
        //        {
        //            if (actualPermutation[actualPosition[k]] < 0)
        //            {
        //                if (actualPosition[k] > 0 && -actualPermutation[actualPosition[k]] > Math.Abs(actualPermutation[actualPosition[k] - 1]))
        //                    break;
        //            }
        //            else
        //            {
        //                if (actualPosition[k] < actualPosition.Length - 1 && actualPermutation[actualPosition[k]] > Math.Abs(actualPermutation[actualPosition[k] + 1]))
        //                    break;
        //            }
        //        }
        //        if (k == -1) break;
        //        for (int i = actualPosition.Length - 1; i > k; i--)
        //            actualPermutation[actualPosition[i]] *= -1;
        //        kIndex = actualPosition[k];*/
        //        kIndex = lastMax = -1;
        //        for (int i = 1; i < actualPermutation.Length; i++)
        //            if (actualPermutation[i] < 0 && -actualPermutation[i] > Math.Abs(actualPermutation[i - 1]) && -actualPermutation[i] > lastMax)
        //            { kIndex = i; lastMax = -actualPermutation[kIndex]; }
        //        for (int i = 0; i < actualPermutation.Length - 1; i++)
        //            if (actualPermutation[i] > 0 && actualPermutation[i] > Math.Abs(actualPermutation[i + 1]) && actualPermutation[i] > lastMax)
        //            { kIndex = i; lastMax = actualPermutation[kIndex]; }
        //        if (kIndex == -1) break;
        //        for (int i = 0; i < actualPermutation.Length; i++)
        //            if (Math.Abs(actualPermutation[i]) > Math.Abs(actualPermutation[kIndex]))
        //                actualPermutation[i] *= -1;
        //        if (Math.Sign(actualPermutation[kIndex]) == 1)
        //        {

        //            int left = kIndex - 1 >= 0 ? clientsNr[Math.Abs(actualPermutation[kIndex - 1]) - 1] + 1 : 0;
        //            int right = kIndex + 2 < actualPermutation.Length ? clientsNr[Math.Abs(actualPermutation[kIndex + 2]) - 1] + 1 : 0;
        //            actualCost += (distances[left, clientsNr[Math.Abs(actualPermutation[kIndex + 1]) - 1] + 1] + distances[clientsNr[actualPermutation[kIndex] - 1] + 1, right]
        //                - distances[left, clientsNr[actualPermutation[kIndex] - 1] + 1] - distances[clientsNr[Math.Abs(actualPermutation[kIndex + 1]) - 1] + 1, right]);
        //            if (actualCost < minCost)
        //                if (CheckTimeWindow(clientsNr, actualPermutation))
        //                {
        //                    minCost = actualCost;
        //                    minPermutation = (int[])actualPermutation.Clone();
        //                }
        //          //  actualPosition[k]++;
        //          //  actualPosition[Math.Abs(actualPermutation[actualPosition[k]]) - 1]--;   //+1 zrobione wcześniej
        //            int c = actualPermutation[kIndex];
        //            actualPermutation[kIndex] = actualPermutation[kIndex + 1];
        //            actualPermutation[kIndex + 1] = c;
        //        }
        //        else
        //        {
        //            int left = kIndex - 2 >= 0 ? clientsNr[Math.Abs(actualPermutation[kIndex - 2]) - 1] + 1 : 0;
        //            int right = kIndex + 1 < actualPermutation.Length ? clientsNr[Math.Abs(actualPermutation[kIndex + 1]) - 1] + 1 : 0;
        //            actualCost += (distances[left, clientsNr[-actualPermutation[kIndex] - 1] + 1] + distances[clientsNr[Math.Abs(actualPermutation[kIndex - 1]) - 1] + 1, right]
        //                - distances[left, clientsNr[Math.Abs(actualPermutation[kIndex - 1]) - 1] + 1] - distances[clientsNr[-actualPermutation[kIndex] - 1] + 1, right]);
        //            if (actualCost < minCost)
        //                if (CheckTimeWindow(clientsNr, actualPermutation))
        //                {
        //                    minCost = actualCost;
        //                    minPermutation = (int[])actualPermutation.Clone();
        //                }
        //           // actualPosition[k]--;
        //          //  actualPosition[Math.Abs(actualPermutation[actualPosition[k]]) - 1]++;   //-1 zrobione wcześniej
        //            int c = actualPermutation[kIndex];
        //            actualPermutation[kIndex] = actualPermutation[kIndex - 1];
        //            actualPermutation[kIndex - 1] = c;
        //        }
        //    }
        //    if (minPermutation!=null)
        //        for (int i = 0; i < minPermutation.Length; i++)
        //            minPermutation[i] = clientsNr[Math.Abs(minPermutation[i]) - 1];
        //    return minCost;
        //}

        //private bool CheckTimeWindow(int[] clients, int[] actualPermutation)
        //{
        //    double timeWindow = description.startTimeDepot;
        //    int lastPoint = 0;
        //    for (int i = 0; i < actualPermutation.Length; i++)
        //    {
        //        int actualClient = clients[Math.Abs(actualPermutation[i]) - 1];
        //        if (description.clients[actualClient].availableTime > timeWindow)
        //            timeWindow = description.clients[actualClient].availableTime;
        //        timeWindow += distances[lastPoint, actualClient + 1];
        //        timeWindow += description.clients[actualClient].durationTime;
        //        if (timeWindow > description.endTimeDepot) return false;
        //        lastPoint = actualClient + 1;
        //    }
        //    timeWindow += distances[lastPoint, 0];
        //    return timeWindow <= description.endTimeDepot;
        //}

        //private double CountCost(int[] clients, int clientsCount, out bool legalResult)
        //{
        //    legalResult = true;
        //    double cost = 0;
        //    double timeWindow = description.startTimeDepot;
        //    int lastPoint = 0;
        //    for (int i = 0; i < clientsCount; i++)
        //    {
        //        int actualClient = clients[i];
        //        if (description.clients[actualClient].availableTime > timeWindow)
        //            timeWindow = description.clients[actualClient].availableTime;
        //        cost += distances[lastPoint, actualClient + 1];
        //        timeWindow += distances[lastPoint, actualClient + 1];
        //        timeWindow += description.clients[actualClient].durationTime;
        //        lastPoint = actualClient + 1;
        //    }
        //    cost += distances[lastPoint, 0];
        //    timeWindow += distances[lastPoint, 0];
        //    legalResult = timeWindow <= description.endTimeDepot;
        //    return cost;
        //}


        private double LittleCarProblem0(List<List<int>> oneDivision, out List<List<int>> oneDivisionMinPermutations)
        {
            int N = oneDivision.Count;
            int K = description.vehiclesCount;
            double oneDivisionMinCost = double.MaxValue;
            oneDivisionMinPermutations = null;
            int[] T = new int[N];
            while (true)
            {
                if (State==TaskSolverState.Timeout)
                {
                    return oneDivisionMinCost;
                }
                List<List<int>> divisionDivision = ConvertToList(T);
                if (divisionDivision.Count == K)
                {
                    List<List<int>> oneDivisionDivisionMinPermutations;
                    double oneDivisionDivisionMinCost = LittleCarProblem1(divisionDivision, oneDivision, out oneDivisionDivisionMinPermutations);
                    if (oneDivisionDivisionMinCost < oneDivisionMinCost)
                    {
                        oneDivisionMinCost = oneDivisionDivisionMinCost;
                        oneDivisionMinPermutations = oneDivisionDivisionMinPermutations;
                    }
                }
                int i;
                for (i = N - 1; i >= 0; --i)
                {
                    int f = T[i];
                    if (f < N - 1)
                    {
                        int k;
                        for (k = 0; k < i; ++k)
                        {
                            if (T[k] == f) break;
                        }
                        if (k >= i) f = N;
                    }
                    if (f < N - 1 && f + 1 < K)
                    {
                        T[i] = f + 1;
                        for (int k = i + 1; k < N; ++k) T[k] = 0;
                        break;
                    }
                }
                if (i < 0) break;
            }
            return oneDivisionMinCost;
        }

        private double LittleCarProblem1(List<List<int>> divisionDivision, List<List<int>> oneDivision, out List<List<int>> oneDivisionMinPermutation)
        {
            double oneDivisionDivisionMinCost = 0;
            oneDivisionMinPermutation = new List<List<int>>();
            for (int i = 0; i < divisionDivision.Count; i++)
            {
                if (State==TaskSolverState.Timeout)
                {
                    oneDivisionMinPermutation = null;
                    return double.MaxValue;
                }
                List<List<int>> actualCar = new List<List<int>>();
                foreach (var setNr in divisionDivision[i])
                    actualCar.Add(oneDivision[setNr]);
                List<int> actualCarBestPermutation;
                double actualCarMinCost = LittleCarProblem2(actualCar, out actualCarBestPermutation);
                if (actualCarMinCost == double.MaxValue)
                {
                    oneDivisionMinPermutation = null;
                    return double.MaxValue;
                }
                oneDivisionDivisionMinCost += actualCarMinCost;
                oneDivisionMinPermutation.Add(actualCarBestPermutation);
            }
            return oneDivisionDivisionMinCost;
        }

        private double LittleCarProblem2(List<List<int>> actualCar, out List<int> actualCarBestPermutation)
        {
            double actualCarMinCost = double.MaxValue;
            actualCarBestPermutation = null;

            int n = actualCar.Count;
            int[] positions = new int[n];
            bool[] used = new bool[n];
            bool last;
            for (int i = 0; i < n; i++)
                positions[i] = i;
            do
            {
                if (State==TaskSolverState.Timeout)
                {
                    return actualCarMinCost;
                }
                List<int> oneSubsetPermutationBestPermutation;
                List<List<int>> subsetsPermutation = GenerateRightPermutation(positions, actualCar);
                double minCost = LittleCarProblem3(subsetsPermutation, out oneSubsetPermutationBestPermutation);

                if (actualCarMinCost > minCost)
                {
                    actualCarMinCost = minCost;
                    actualCarBestPermutation = oneSubsetPermutationBestPermutation;
                }
                last = false;
                int k = n - 2;
                while (k >= 0)
                {
                    if (positions[k] < positions[k + 1])
                    {
                        for (int i = 0; i < n; i++)
                            used[i] = false;
                        for (int i = 0; i < k; i++)
                            used[positions[i]] = true;
                        do positions[k]++; while (used[positions[k]]);
                        used[positions[k]] = true;
                        for (int i = 0; i < n; i++)
                            if (!used[i]) positions[++k] = i;
                        break;
                    }
                    else k--;
                }
                last = (k < 0);
            } while (!last);
            return actualCarMinCost;
        }

        private double LittleCarProblem3(List<List<int>> subsetsPermutation, out List<int> oneSubsetPermutationBestPermutation)
        {
            oneSubsetPermutationBestPermutation = null;
            double actualMin = double.MaxValue;
            LittleCarProblem4(0, subsetsPermutation, new List<int>(), description.startTimeDepot, 0, ref oneSubsetPermutationBestPermutation, ref actualMin);
            return actualMin;
        }

        private void LittleCarProblem4(int index, List<List<int>> subsetsPermutation, List<int> actualPermutation, double timeWindow, double actualCost, ref List<int> oneSubsetPermutationBestPermutation, ref double actualMin)
        {
            if (index == subsetsPermutation.Count - 1)
            {
                // double localMinCost = double.MaxValue;
                //  oneSubsetPermutationBestPermutation = null;
                int n = subsetsPermutation[index].Count;
                int[] positions = new int[n];
                bool[] used = new bool[n];
                bool last;
                for (int i = 0; i < n; i++)
                    positions[i] = i;
                do
                {

                    List<int> rightPermutation = GenerateRightPermutation(positions, subsetsPermutation[index]);
                    double newTimeWindow = timeWindow;
                    double lastCost = CountPermutation(rightPermutation, ref newTimeWindow, actualCost);

                    if (actualMin > lastCost)
                    {
                        actualMin = lastCost;
                        oneSubsetPermutationBestPermutation = new List<int>(actualPermutation);
                        oneSubsetPermutationBestPermutation.AddRange(rightPermutation);
                    }
                    if (State==TaskSolverState.Timeout)
                    {
                        return;
                    }
                    last = false;
                    int k = n - 2;
                    while (k >= 0)
                    {
                        if (positions[k] < positions[k + 1])
                        {
                            for (int i = 0; i < n; i++)
                                used[i] = false;
                            for (int i = 0; i < k; i++)
                                used[positions[i]] = true;
                            do positions[k]++; while (used[positions[k]]);
                            used[positions[k]] = true;
                            for (int i = 0; i < n; i++)
                                if (!used[i]) positions[++k] = i;
                            break;
                        }
                        else k--;
                    }
                    last = (k < 0);
                } while (!last);
                return;
            }
            else
            {
                //double localMinCost = double.MaxValue;
                //oneSubsetPermutationBestPermutation = null;
                int n = subsetsPermutation[index].Count;
                int[] positions = new int[n];
                bool[] used = new bool[n];
                bool last;
                for (int i = 0; i < n; i++)
                    positions[i] = i;
                do
                {
                    List<int> rightPermutation = GenerateRightPermutation(positions, subsetsPermutation[index]);
                    double newTimeWindow = timeWindow;
                    double lastCost = CountPermutation(rightPermutation, ref newTimeWindow, actualCost);

                    if (actualMin > lastCost)
                    {
                        var actual = new List<int>(actualPermutation);
                        actual.AddRange(rightPermutation);
                        actual.Add(-1);
                        LittleCarProblem4(index + 1, subsetsPermutation, actual, newTimeWindow, lastCost, ref oneSubsetPermutationBestPermutation, ref actualMin);
                    }
                    if (State == TaskSolverState.Timeout)
                    {
                        return;
                    }
                    last = false;
                    int k = n - 2;
                    while (k >= 0)
                    {
                        if (positions[k] < positions[k + 1])
                        {
                            for (int i = 0; i < n; i++)
                                used[i] = false;
                            for (int i = 0; i < k; i++)
                                used[positions[i]] = true;
                            do positions[k]++; while (used[positions[k]]);
                            used[positions[k]] = true;
                            for (int i = 0; i < n; i++)
                                if (!used[i]) positions[++k] = i;
                            break;
                        }
                        else k--;
                    }
                    last = (k < 0);
                } while (!last);
            }
        }

        private double CountPermutation(List<int> onePermutation, ref double timeWindow, double actualCost)
        {
            double local;
            Point lastPoint = description.coordinateDepot;
            for (int i = 0; i < onePermutation.Count; i++)
            {
                if (State == TaskSolverState.Timeout)
                    return double.MaxValue;
                Client actualClient = description.clients[onePermutation[i]];

                if (timeWindow < actualClient.availableTime) timeWindow = actualClient.availableTime;

                local = description.CountDistance(lastPoint, actualClient.coordinate);
                timeWindow += local;
                timeWindow += actualClient.durationTime;
                actualCost += local;
                lastPoint = actualClient.coordinate;
                if (timeWindow > description.endTimeDepot) return double.MaxValue;
            }
            local = description.CountDistance(lastPoint, description.coordinateDepot);
            timeWindow += local;
            actualCost += local;
            return timeWindow > description.endTimeDepot ? double.MaxValue : actualCost;
        }

        private List<List<int>> ConvertToList(int[] tab)
        {
            int max = tab.Max();
            List<List<int>> allSets = new List<List<int>>();
            for (int i = 0; i < max; i++)
            {
                allSets[i] = new List<int>();
            }
            for (int j = 0; j < tab.Length; j++)
                allSets[tab[j]].Add(j);
            return allSets;
        }

        private List<List<int>> GenerateRightPermutation(int[] positions, List<List<int>> actualCar)
        {
            List<List<int>> result = new List<List<int>>();
            for (int i = 0; i < positions.Length; i++)
                result.Add(actualCar[positions[i]]);
            return result;
        }

        private List<int> GenerateRightPermutation(int[] positions, List<int> oneDivision)
        {
            int[] result = new int[oneDivision.Count];
            for (int i = 0; i < positions.Length; i++)
                result[i] = positions[i] == -1 ? -1 : oneDivision[positions[i]];
            return result.ToList();
        }
    }
}
