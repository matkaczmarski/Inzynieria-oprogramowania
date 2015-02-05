using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UCCTaskSolver;

namespace FileManager
{
    public class SolutionDescriptionToFileParser
    {
        public void WriteSolutionToFile(SolutionDescription _solutionDescription, string path)
        {
            try
            {
                StreamWriter streamWriter = new StreamWriter(path);
                for (int i = 0; i < _solutionDescription.m_permutation.Length; i++)
                {
                    if (_solutionDescription.m_permutation[i].Length == 0)
                        streamWriter.WriteLine("Vehicle {0} has no clients to visit.", i);
                    else
                    {
                        streamWriter.WriteLine("Vehicle {0} visits clients:", i);
                        foreach (int client in _solutionDescription.m_permutation[i])
                        {
                            streamWriter.WriteLine("    {0}", client);
                        }
                    }
                }
                streamWriter.WriteLine();
                streamWriter.WriteLine("Total cost: {0}", _solutionDescription.m_result);
                streamWriter.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }


        public void WriteSolutionToFile(int[][] permutations, double result, string path)
        {
            try
            {
                StreamWriter streamWriter = new StreamWriter(path);
                for (int i = 0; i < permutations.Length; i++)
                {
                    if (permutations[i].Length == 0)
                        streamWriter.WriteLine("Vehicle {0} has no clients to visit.", i);
                    else
                    {
                        streamWriter.WriteLine("Vehicle {0} visits clients:", i);
                        foreach (int client in permutations[i])
                        {
                            streamWriter.WriteLine("    {0}", client);
                        }
                    }
                }
                streamWriter.WriteLine();
                streamWriter.WriteLine("Total cost: {0}", result);
                streamWriter.Close();
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
            }
        }
    }
}
