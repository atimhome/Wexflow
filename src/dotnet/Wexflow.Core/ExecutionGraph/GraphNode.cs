using System.Runtime.Serialization;

namespace Wexflow.Core.ExecutionGraph
{
    /// <summary>
    /// GraphNode.
    /// </summary>
    [DataContract]
    public class GraphNode : Service.Contracts.Node
    {

        /// <summary>
        /// Node description
        /// </summary>
        [DataMember]
        public string Description { get; private set; }

        /// <summary>
        /// Creates a new node.
        /// </summary>
        /// <param name="id">Node id.</param>
        /// <param name="parentId">Node parent id.</param>
        /// <param name="name">Node name.</param>
        /// <param name="description">Node description.</param>
        public GraphNode(string id, string name, string parentId, string description = "") : base(id, name, parentId)
        {            
            this.Description = description;
        }
    }
}
