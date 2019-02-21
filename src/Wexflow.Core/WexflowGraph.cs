using System.Collections.Generic;
using System.Linq;
using Wexflow.Core.ExecutionGraph;
using Wexflow.Core.ExecutionGraph.Flowchart;

namespace Wexflow.Core
{
    /// <summary>
    /// WexflowGraph
    /// </summary>
    public class WexflowGraph
    {
        private Task[] _Tasks { get; set; }       
        
        private Node[] _Nodes { get; set; }

        /// <summary>
        /// Creates a new instance of Wexflow Graph.
        /// </summary>
        /// <param name="wf">Workflow</param>
        public WexflowGraph(Workflow wf)
        {
            this._Tasks = wf.Taks;
            this._Nodes = wf.ExecutionGraph.Nodes;
        }

        /// <summary>
        /// Get the Graph Nodes from Workflow
        /// </summary>
        /// <returns>Graph Nodes</returns>
        public GraphNode[] GetGraphNodes()
        {
            string childParentId;
            GraphNode[] output = this._GetExecutionGraphNodes(this._Nodes, out childParentId).ToArray();
            return output;
        }

        private IList<GraphNode> _GetExecutionGraphNodes(Node[] nodesArray, out string childParentId, string parentLayer = "", string selfLayer = "")
        {
            IList<GraphNode> dataList = new List<GraphNode>();
            childParentId = string.Empty;
            string lastConditionNodeId = string.Empty;

            foreach (var node in nodesArray)
            {
                // 正常狀況(多層判斷,如果有值表示有子層架構)
                if (string.IsNullOrWhiteSpace(lastConditionNodeId))
                {
                    dataList = dataList.Concat(this._GetExecutionGraphNode(node, out lastConditionNodeId, parentLayer, selfLayer)).ToList();
                    continue;
                }

                List<string> parentIdList = lastConditionNodeId.Split('&').ToList();
                lastConditionNodeId = string.Empty;
                node.ParentId = -2;
                dataList = dataList.Concat(this._GetExecutionGraphNode(node, out lastConditionNodeId, parentLayer, selfLayer)).ToList();

                // 將if else的線拉回至此節點
                foreach (var pid in parentIdList)
                {
                    dataList.Add(new GraphNode(dataList.Last().Id, dataList.Last().Name, pid));
                }
            }

            // 最後一個節點判斷
            if (string.IsNullOrWhiteSpace(lastConditionNodeId) == false)
            {
                // 將資料送到上一層
                childParentId = lastConditionNodeId;
            }

            return dataList;
        }

        private IList<GraphNode> _GetExecutionGraphNode(Node node, out string nextNodeParentId, string parentLayer = "", string selfLayer = "")
        {
            IList<GraphNode> dataList = new List<GraphNode>();
            string layer = parentLayer + "n";
            nextNodeParentId = string.Empty;            

            if (node is If)
            {
                //nodeName = "If...EndIf";
                dataList = this._GetExecutionGraphIfNode(node, out nextNodeParentId, parentLayer, selfLayer);
                return dataList;
            }

            if (node is While)
            {
                //nodeName = "While...EndWhile";
                dataList = this._GetExecutionGraphWhileNode(node, out nextNodeParentId, parentLayer, selfLayer);
                return dataList;
            }

            if (node is Switch)
            {
                //nodeName = "Switch...EndSwitch";
                dataList = this._GetExecutionGraphSwitchNode(node, out nextNodeParentId, parentLayer, selfLayer);
                return dataList;
            }

            // Core.ExecutionGraph.Node
            string nodeId = layer + node.Id + selfLayer;
            string parentId = (node.ParentId == -2) ? "n-1" : (node.ParentId == -1) ? (string.IsNullOrEmpty(parentLayer)) ? "n-1" : parentLayer : layer + node.ParentId + selfLayer;
            string description = (nodeId.IndexOf(parentId) == -1) ? string.Empty : selfLayer;

            dataList.Add(new GraphNode(nodeId, this._GetTaskName(node.Id), parentId, description));

            return dataList;
        }

        private IList<GraphNode> _GetExecutionGraphIfNode(Node node, out string nextNodeParentId, string parentLayer = "", string selfLayer = "")
        {
            IList<GraphNode> dataList = new List<GraphNode>();
            string layer = parentLayer + "n";
            nextNodeParentId = string.Empty;
            If ifNode = node as If;

            string ifConditionId = layer + ifNode.Id + selfLayer;

            // Add DO and Else Nodes(加入DO跟Else的節點)            
            string childParentId = string.Empty;

            dataList = dataList.Concat(this._GetExecutionGraphNodes(ifNode.DoNodes, out childParentId, ifConditionId, "Do")).ToList();
            if (string.IsNullOrWhiteSpace(childParentId) == false)
            {
                // In Function,there are some conditions.
                nextNodeParentId += "&" + childParentId;
            }

            dataList = dataList.Concat(this._GetExecutionGraphNodes(ifNode.ElseNodes, out childParentId, ifConditionId, "Else")).ToList();
            if (string.IsNullOrWhiteSpace(childParentId) == false)
            {
                // In Function,there are some conditions.
                nextNodeParentId += "&" + childParentId;
            }

            // Get the last nodes id,to Combine the if nodes to next node            
            nextNodeParentId = this._GetLastNodeID(ifConditionId, ifNode.DoNodes, "Do", nextNodeParentId);
            nextNodeParentId = this._GetLastNodeID(ifConditionId, ifNode.ElseNodes, "Else", nextNodeParentId);

            // Remove first "&"
            nextNodeParentId = (nextNodeParentId.Length > 0) ? nextNodeParentId.Remove(0, 1) : nextNodeParentId;

            // Add IF Condition Node(加入IF判斷式的節點)
            string ifConditionParentId = (node.ParentId == -2) ? "n-1" : (node.ParentId == -1) ? (string.IsNullOrEmpty(parentLayer)) ? "n-1" : parentLayer : layer + ifNode.ParentId + selfLayer;
            string description = (ifConditionId.IndexOf(ifConditionParentId) == -1) ? string.Empty : selfLayer;

            dataList.Add(new GraphNode(ifConditionId, this._GetTaskName(ifNode.IfId), ifConditionParentId, description));

            return dataList;
        }

        private IList<GraphNode> _GetExecutionGraphWhileNode(Node node, out string nextNodeParentId, string parentLayer = "", string selfLayer = "")
        {
            IList<GraphNode> dataList = new List<GraphNode>();
            string layer = parentLayer + "n";
            nextNodeParentId = string.Empty;
            While whileNode = node as While;

            // Add While Condition Node(加入While判斷式的節點)       
            string whileConditionId = layer + whileNode.Id + selfLayer;            
            string whileConditionParentId = (node.ParentId == -2) ? "n-1" : (node.ParentId == -1) ? (string.IsNullOrEmpty(parentLayer)) ? "n-1" : parentLayer : layer + whileNode.ParentId + selfLayer;
            string whileConditionTaskName = this._GetTaskName(whileNode.WhileId);
            string description = (whileConditionId.IndexOf(whileConditionParentId) == -1) ? string.Empty : selfLayer;
            dataList.Add(new GraphNode(whileConditionId, whileConditionTaskName, whileConditionParentId, description));

            // Add While Child Nodes(加入子節點)
            Node[] whileChildNodes = new Node[whileNode.Nodes.Count()];
            whileNode.Nodes.CopyTo(whileChildNodes, 0);

            string childParentId = string.Empty;

            dataList = dataList.Concat(
                this._GetExecutionGraphNodes(whileChildNodes, out childParentId, whileConditionId, "while")
            ).ToList();

            // Get the child last nodes id,to Combine the nodes to next node    
            if (string.IsNullOrWhiteSpace(childParentId) == false)
            {
                // In Function,there are some conditions.
                nextNodeParentId += "&" + childParentId;
            }

            // Get the last node id,to return the node to condition node
            nextNodeParentId = this._GetLastNodeID(whileConditionId, whileNode.Nodes, "while", nextNodeParentId);
            nextNodeParentId = (nextNodeParentId.Length > 0) ? nextNodeParentId.Remove(0, 1) : nextNodeParentId;

            List<string> parentIdList = nextNodeParentId.Split('&').ToList();
            foreach (var parentId in parentIdList)
            {
                dataList.Add(new GraphNode(whileConditionId, whileConditionTaskName, parentId));
            }
            nextNodeParentId = string.Empty;

            return dataList;
        }

        private IList<GraphNode> _GetExecutionGraphSwitchNode(Node node, out string nextNodeParentId, string parentLayer = "", string selfLayer = "")
        {
            IList<GraphNode> dataList = new List<GraphNode>();
            string layer = parentLayer + "n";
            nextNodeParentId = string.Empty;
            Switch switchNode = node as Switch;

            // Add Cases Nodes(加入Cases的節點)
            string switchConditionId = layer + switchNode.Id + selfLayer;
            string childParentId = string.Empty;

            dataList = dataList.Concat(
                    this._GetExecutionGraphNodes(switchNode.Default, out childParentId, switchConditionId, "default")
                ).ToList();

            // Get the child last nodes id,to Combine the nodes to next node    
            if (string.IsNullOrWhiteSpace(childParentId) == false)
            {
                // In Function,there are some conditions.
                nextNodeParentId += "&" + childParentId;
            }

            nextNodeParentId = this._GetLastNodeID(switchConditionId, switchNode.Default, "default", nextNodeParentId);


            foreach (var switchCase in switchNode.Cases)
            {
                dataList = dataList.Concat(
                    this._GetExecutionGraphNodes(switchCase.Nodes, out childParentId, switchConditionId, switchCase.Value)
                ).ToList();

                // Get the child last nodes id,to Combine the nodes to next node    
                if (string.IsNullOrWhiteSpace(childParentId) == false)
                {
                    // In Function,there are some conditions.
                    nextNodeParentId += "&" + childParentId;
                }

                // Get the last nodes id,to Combine the switch nodes to next node
                nextNodeParentId = this._GetLastNodeID(switchConditionId, switchCase.Nodes, switchCase.Value, nextNodeParentId);
            }
           
            // Remove first "&"
            nextNodeParentId = (nextNodeParentId.Length > 0) ? nextNodeParentId.Remove(0, 1) : nextNodeParentId;

            // Add Switch Condition Node(加入Switch判斷式的節點)            
            string switchConditionParentId = (node.ParentId == -2) ? "n-1" : (node.ParentId == -1) ? (string.IsNullOrEmpty(parentLayer)) ? "n-1" : parentLayer : layer + switchNode.ParentId + selfLayer;
            string description = (switchConditionId.IndexOf(switchConditionParentId) == -1) ? string.Empty : selfLayer;

            dataList.Add(new GraphNode(switchConditionId, this._GetTaskName(switchNode.SwitchId), switchConditionParentId, description));

            return dataList;
        }

        private string _GetLastNodeID(string layer, Node[] nodes, string type, string nextNodeParentId)
        {
            string childLayer = layer + "n";
            var lastNode = nodes.Last();

            if (lastNode is If == false && lastNode is Switch == false)
                nextNodeParentId += "&" + childLayer + lastNode.Id + type;

            return nextNodeParentId;
        }

        private string _GetTaskName(int taskId)
        {
            var task = this._Tasks.FirstOrDefault(t => t.Id == taskId);
            return "Task " + taskId + (task != null ? ": " + task.Description : "");
             
        }
    }
}
