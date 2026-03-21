// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections;
using System.Collections.Generic;
using AstNode = global::NCSDecomp.Core.Node.Node;

namespace NCSDecomp.Core.Utils
{
    /// <summary>
    /// Per-node metadata for analysis passes (DeNCS NodeAnalysisData.java).
    /// </summary>
    public class NodeAnalysisData
    {
        private Hashtable _nodeDataHash = new Hashtable(1);

        public void Close()
        {
            if (_nodeDataHash != null)
            {
                foreach (NodeData data in _nodeDataHash.Values)
                {
                    data.Close();
                }

                _nodeDataHash = null;
            }
        }

        public void SetPos(AstNode node, int pos)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                data = new NodeData(pos);
                _nodeDataHash[node] = data;
            }
            else
            {
                data.Pos = pos;
            }
        }

        public int GetPos(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to read position on a node not in the hashtable.");
            }

            return data.Pos;
        }

        public void SetDestination(AstNode jump, AstNode destination)
        {
            NodeData data = (NodeData)_nodeDataHash[jump];
            if (data == null)
            {
                data = new NodeData();
                data.JumpDestination = destination;
                _nodeDataHash[jump] = data;
            }
            else
            {
                data.JumpDestination = destination;
            }
        }

        public AstNode GetDestination(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to read destination on a node not in the hashtable.");
            }

            return data.JumpDestination;
        }

        public void SetCodeState(AstNode node, byte state)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                data = new NodeData();
                data.State = state;
                _nodeDataHash[node] = data;
            }
            else
            {
                data.State = state;
            }
        }

        public void DeadCode(AstNode node, bool deadcode)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to set status on a node not in the hashtable.");
            }

            data.State = deadcode ? NodeData.StateDead : NodeData.StateNormal;
        }

        public bool DeadCode(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to read status on a node not in the hashtable.");
            }

            return data.State == NodeData.StateDead || data.State == NodeData.StateDeadProcess;
        }

        public bool ProcessCode(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to read status on a node not in the hashtable.");
            }

            return data.State != NodeData.StateDead;
        }

        public void LogOrCode(AstNode node, bool logor)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to set status on a node not in the hashtable.");
            }

            data.State = logor ? NodeData.StateLogor : NodeData.StateNormal;
        }

        public bool LogOrCode(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to read status on a node not in the hashtable.");
            }

            return data.State == NodeData.StateLogor;
        }

        public void AddOrigin(AstNode node, AstNode origin)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                data = new NodeData();
                data.AddOrigin(origin);
                _nodeDataHash[node] = data;
            }
            else
            {
                data.AddOrigin(origin);
            }
        }

        public AstNode RemoveLastOrigin(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                throw new InvalidOperationException("Attempted to read origin on a node not in the hashtable.");
            }

            if (data.Origins == null || data.Origins.Count == 0)
            {
                return null;
            }

            int last = data.Origins.Count - 1;
            AstNode r = data.Origins[last];
            data.Origins.RemoveAt(last);
            return r;
        }

        public void SetStack(AstNode node, object stack, bool overwrite)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            if (data == null)
            {
                data = new NodeData();
                data.Stack = stack;
                _nodeDataHash[node] = data;
            }
            else if (data.Stack == null || overwrite)
            {
                data.Stack = stack;
            }
        }

        public object GetStack(AstNode node)
        {
            NodeData data = (NodeData)_nodeDataHash[node];
            return data != null ? data.Stack : null;
        }

        public void ClearProtoData()
        {
            foreach (NodeData d in _nodeDataHash.Values)
            {
                d.Stack = null;
            }
        }

        public class NodeData
        {
            public const byte StateNormal = 0;
            public const byte StateDead = 1;
            public const byte StateLogor = 2;
            public const byte StateDeadProcess = 3;

            public int Pos;
            public AstNode JumpDestination;
            public object Stack;
            public byte State;
            public List<AstNode> Origins;

            public NodeData()
            {
                Pos = -1;
                JumpDestination = null;
                Stack = null;
                State = 0;
            }

            public NodeData(int pos)
            {
                JumpDestination = null;
                Pos = pos;
                Stack = null;
                State = 0;
            }

            public void AddOrigin(AstNode origin)
            {
                if (Origins == null)
                {
                    Origins = new List<AstNode>();
                }

                Origins.Add(origin);
            }

            public void Close()
            {
                JumpDestination = null;
                Stack = null;
                Origins = null;
            }
        }
    }
}
