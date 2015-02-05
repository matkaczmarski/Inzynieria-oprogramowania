
using SE_lab.Messages;
using System;

namespace SE_lab
{
    public class ComputationalNode : ClusterElement
    {
    }

    public abstract class ClusterElement
    {
        public ulong Id { set; get; }
        public DateTime lastStatus { set; get; }
        public StatusThread[] statusThreads { set; get; }
        public string[] solvableProblems { set; get; }
        public ComponentObject state { set; get; }
    }
}
