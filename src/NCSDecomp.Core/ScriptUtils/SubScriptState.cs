// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.
//
// Port of DeNCS SubScriptState.java (mechanical conversion + C# adjustments).

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using NCSDecomp.Core;
using NCSDecomp.Core.Node;
using NCSDecomp.Core.ScriptNode;
using NCSDecomp.Core.Stack;
using NCSDecomp.Core.Utils;
using AstNode = global::NCSDecomp.Core.Node.Node;
using ScriptDomNode = global::NCSDecomp.Core.ScriptNode.ScriptNode;

namespace NCSDecomp.Core.ScriptUtils
{
    public class SubScriptState
    {

        private const sbyte StateDone = -1;
        private const byte StateNormal = 0;
        private const byte StateInMod = 1;
        private const byte StateInActionArg = 2;
        private const byte StateWhileCond = 3;
        private const byte StateSwitchCases = 4;
        private const byte StateInPrefixStack = 5;
        private ASub root;
        private ScriptRootNode current;
        private sbyte state;
        private NodeAnalysisData nodedata;
        private SubroutineAnalysisData subdata;
        private ActionsData actions;
        private LocalVarStack stack;
        private string varprefix;
        private Hashtable vardecs;
        private Hashtable varcounts;
        private Hashtable varnames;
        private readonly bool preferSwitches;

        public SubScriptState(NodeAnalysisData nodedata, SubroutineAnalysisData subdata, LocalVarStack stack,
              SubroutineState protostate, ActionsData actions, bool preferSwitches)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            state = 0;
            vardecs = new Hashtable();
            this.stack = stack;
            varcounts = new Hashtable();
            varprefix = "";
            root = new ASub(protostate.Type(), protostate.GetId(), GetParams(protostate.GetParamCount()),
                  protostate.GetStart(), protostate.GetEnd());
            current = root;
            varnames = new Hashtable();
            this.actions = actions;
            this.preferSwitches = preferSwitches;
        }

        public SubScriptState(NodeAnalysisData nodedata, SubroutineAnalysisData subdata, LocalVarStack stack, bool preferSwitches)
        {
            this.nodedata = nodedata;
            this.subdata = subdata;
            state = 0;
            vardecs = new Hashtable();
            root = new ASub(0, 0);
            current = root;
            this.stack = stack;
            varcounts = new Hashtable();
            varprefix = "";
            varnames = new Hashtable();
            this.preferSwitches = preferSwitches;
        }

        public void SetVarPrefix(string prefix)
        {
            varprefix = prefix;
        }

        public void SetStack(LocalVarStack stack)
        {
            this.stack = stack;
        }

        public void ParseDone()
        {
            nodedata = null;
            subdata = null;
            if (stack != null)
            {
                stack.DoneParse();
            }

            stack = null;
            if (vardecs != null)
            {
                foreach (DictionaryEntry __de in vardecs)
                {
                    Variable var = (Variable)__de.Key;
                    var.DoneParse();
                }
            }


        }

        public void Close()
        {
            if (vardecs != null)
            {
                foreach (DictionaryEntry __de in vardecs)
                {
                    Variable var = (Variable)__de.Key;
                    var.Close();
                }

                vardecs = null;
            }

            varcounts = null;
            varnames = null;
            if (root != null)
            {
                root.Close();
            }

            current = null;
            root = null;
            nodedata = null;
            subdata = null;
            actions = null;
            if (stack != null)
            {
                stack.Close();
                stack = null;
            }


        }

        public string toString()
        {
            return root.ToString();
        }

        public string ToStringGlobals()
        {
            return root.GetBody();
        }

        public string GetProto()
        {
            return root.GetHeader();
        }

        public ASub GetRoot()
        {
            return root;
        }

        public string GetName()
        {
            return root.Name();
        }

        public void SetName(string name)
        {
            root.Name(name);
        }

        public List<Variable> GetVariables()
        {
            var vars = new List<Variable>();
            foreach (DictionaryEntry e in vardecs)
            {
                vars.Add((Variable)e.Key);
            }

            var varstructs = new List<VarStruct>();
            for (int vi = vars.Count - 1; vi >= 0; vi--)
            {
                Variable v = vars[vi];
                if (v.IsStruct())
                {
                    varstructs.Add(v.Varstruct());
                    vars.RemoveAt(vi);
                }
            }

            vars.AddRange(varstructs);
            vars.AddRange(root.GetParamVars());
            return vars;
        }

        public void IsMain(bool ismain)
        {
            root.IsMain(ismain);
        }

        public bool IsMain()
        {
            return root.IsMain();
        }

        private void AssertState(AstNode node)
        {
            if (state != 0)
            {
                if (state == 2 && !(node is AJumpCommand))
                {
                    throw new InvalidOperationException("In action arg, expected JUMP at node " + node);
                }
                else if (state == StateDone)
                {
                    throw new InvalidOperationException("In DONE state, no more nodes expected at node " + node);
                }
                else if (state == 5 && !(node is ACopyTopSpCommand))
                {
                    throw new InvalidOperationException("In prefix stack op state, expected CPTOPSP at node " + node);
                }
            }
        }

        private void CheckStart(AstNode node)
        {
            AssertState(node);
            int nodePos = nodedata.GetPos(node);
            SubScriptLogger.Trace("checkStart: pos=" + nodePos + ", current=" + current.GetType().Name +
                  ", hasChildren=" + current.HasChildren());

            if (current.HasChildren())
            {
                ScriptDomNode lastNode = current.GetLastChild();
                SubScriptLogger.Trace("checkStart: lastChild=" + (lastNode != null ? lastNode.GetType().Name : "null"));

                if ((lastNode is ASwitch)
                      && nodedata.GetPos(node) == ((ASwitch)lastNode).GetFirstCaseStart())
                {
                    int firstCaseStart = ((ASwitch)lastNode).GetFirstCaseStart();
                    SubScriptLogger.Trace("checkStart: entering first switch case (firstCaseStart=" + firstCaseStart + ")");
                    current = ((ASwitch)lastNode).GetFirstCase();
                }
                else if ((lastNode is ASwitch))
                {
                    int firstCaseStart = ((ASwitch)lastNode).GetFirstCaseStart();
                    SubScriptLogger.Trace("checkStart: lastChild is ASwitch but nodePos (" + nodePos + ") != firstCaseStart (" + firstCaseStart + ")");
                }
            }
        }

        private void CheckEnd(AstNode node)
        {
            int nodePos = nodedata.GetPos(node);
            SubScriptLogger.Trace("checkEnd: START pos=" + nodePos + ", current=" + current.GetType().Name +
                  ", currentEnd=" + current.GetEnd() + ", state=" + state);

            while (current != null)
            {
                if (nodedata.GetPos(node) != current.GetEnd())
                {
                    SubScriptLogger.Trace("checkEnd: nodePos != currentEnd, returning early");
                    return;
                }

                SubScriptLogger.Trace("checkEnd: nodePos == currentEnd, processing " + current.GetType().Name);

                if ((current is ASwitchCase))
                {
                    SubScriptLogger.Trace("checkEnd: current is ASwitchCase");
                    ASwitchCase nextCase = ((ASwitch)current.Parent()).GetNextCase((ASwitchCase)current);
                    if (nextCase != null)
                    {
                        SubScriptLogger.Trace("checkEnd: moving to next case");
                        current = nextCase;
                    }
                    else
                    {
                        ScriptRootNode afterSwitch = (ScriptRootNode)current.Parent().Parent();
                        SubScriptLogger.Trace("checkEnd: no next case, moving to " + (afterSwitch != null ? afterSwitch.GetType().Name : "null"));
                        current = afterSwitch;
                    }

                    nextCase = null;
                    return;
                }

                if ((current is AIf))
                {
                    SubScriptLogger.Trace("checkEnd: current is AIf, checking for else block");
                    AstNode dest = nodedata.GetDestination(node);
                    if (dest == null)
                    {
                        SubScriptLogger.Trace("checkEnd: dest is null, returning");
                        return;
                    }

                    // Get the destination position and AIf end position
                    int destPos = nodedata.GetPos(dest);
                    int aifEnd = current.GetEnd();
                    SubScriptLogger.Trace("checkEnd: AIf end=" + aifEnd + ", destPos=" + destPos + ", expectedElseStart=" + (aifEnd + 6));

                    // If the destination is exactly 6 bytes after the AIf's end, there's no else block
                    // If the destination is before the AIf's end, it's a backward jump (e.g., loop back) - no else block
                    // Only create an else block if destPos > aifEnd + 6 (forward jump past the if block)
                    if (destPos > aifEnd + 6)
                    {
                        SubScriptLogger.Trace("checkEnd: destPos > aifEnd+6, creating else block");
                        // Check if this AIf is inside an AElse (else-if chain)
                        // If so, create the next AElse as a sibling of the parent AElse, not nested
                        ScriptRootNode parent = (ScriptRootNode)current.Parent();
                        SubScriptLogger.Trace("checkEnd: AIf parent=" + (parent != null ? parent.GetType().Name : "null"));

                        // Safety check: don't create AElse if AIf is at root level (shouldn't happen, but protect against it)
                        if (parent == null || (current is ASub))
                        {
                            SubScriptLogger.Trace("checkEnd: AIf at root level, not creating else");
                            dest = null;
                            return;
                        }

                        ScriptRootNode elseParent = parent;

                        // Check if this AIf is inside an AElse (else-if chain case)
                        // If the parent is an AElse and the AIf is the last/only child,
                        // the next AElse should be a sibling of the parent AElse, not nested
                        if ((parent is AElse))
                        {
                            SubScriptLogger.Trace("checkEnd: AIf is inside AElse, checking else-if chain");
                            // Check if this AIf is the last child of the AElse
                            bool isLastChild = parent.HasChildren() && parent.GetLastChild() == current;
                            SubScriptLogger.Trace("checkEnd: isLastChild=" + isLastChild + ", parentEnd=" + parent.GetEnd());

                            if (isLastChild)
                            {
                                // Check if this is a pure else-if chain (AIf's else ends at parent AElse's end)
                                // or a nested if-else (there's more content in parent AElse after this AIf's else)
                                //
                                // For pure else-if: destPos is at or past parent's end → use grandParent (siblings)
                                // For nested: destPos is before parent's end → use parent (keep nested)
                                int parentEnd = parent.GetEnd();

                                // If the JMP destination is at or past the parent AElse's end,
                                // this is a continuation of the else-if chain at the same level
                                if (destPos >= parentEnd)
                                {
                                    ScriptRootNode grandParent = (ScriptRootNode)parent.Parent();
                                    SubScriptLogger.Trace("checkEnd: destPos >= parentEnd, using grandParent=" +
                                          (grandParent != null ? grandParent.GetType().Name : "null"));
                                    if (grandParent != null)
                                    {
                                        // Verify that the parent AElse is actually the last child of grandParent
                                        // This ensures the new AElse will come immediately after the parent AElse
                                        bool parentIsLastChild = grandParent.HasChildren() &&
                                              grandParent.GetLastChild() == parent;
                                        if (parentIsLastChild)
                                        {
                                            elseParent = grandParent;
                                            SubScriptLogger.Trace("checkEnd: parent AElse is last child of grandParent, using grandParent");
                                        }
                                        else
                                        {
                                            SubScriptLogger.Trace("checkEnd: parent AElse is NOT last child of grandParent, keeping parent");
                                            // Keep elseParent = parent to avoid adding AElse in wrong position
                                        }
                                    }
                                }
                                else
                                {
                                    SubScriptLogger.Trace("checkEnd: destPos < parentEnd, keeping nested (elseParent=parent)");
                                }
                                // Otherwise, the else block is nested inside the parent AElse (has siblings)
                                // Keep elseParent = parent
                            }
                            else
                            {
                                SubScriptLogger.Trace("checkEnd: AIf not last child, keeping nested");
                            }
                            // If the AIf is not the last child, keep parent as elseParent (nested else)
                        }
                        else
                        {
                            SubScriptLogger.Trace("checkEnd: AIf parent is not AElse, using parent as elseParent");
                        }

                        // Safety check: ensure elseParent is not null and is not the AIf itself
                        if (elseParent == null || elseParent == current)
                        {
                            SubScriptLogger.Trace("checkEnd: elseParent invalid, using root");
                            elseParent = root;
                        }

                        // CRITICAL VALIDATION: An AElse must ALWAYS immediately follow an AIf in the output.
                        // The AIf we're processing (or its containing AElse) MUST be the last child of elseParent.
                        // Otherwise, there will be other nodes between the AIf and AElse, causing a syntax error.
                        bool canAddElse = false;

                        if (!elseParent.HasChildren())
                        {
                            // No children - can't add AElse here (would be first child, no preceding AIf)
                            SubScriptLogger.Trace("checkEnd: elseParent has no children - cannot add AElse");
                            canAddElse = false;
                        }
                        else
                        {
                            ScriptDomNode lastChild = elseParent.GetLastChild();

                            // Case 1: The AIf itself is the last child of elseParent
                            if (lastChild == current)
                            {
                                canAddElse = true;
                                SubScriptLogger.Trace("checkEnd: AIf is last child of elseParent - can add AElse");
                            }
                            // Case 2: The parent AElse (containing the AIf) is the last child of elseParent
                            else if ((parent is AElse) && lastChild == parent)
                            {
                                canAddElse = true;
                                SubScriptLogger.Trace("checkEnd: parent AElse is last child of elseParent - can add AElse");
                            }
                            // Case 3: The last child is an AIf (for regular if-else, not else-if)
                            else if ((lastChild is AIf))
                            {
                                canAddElse = true;
                                SubScriptLogger.Trace("checkEnd: last child is AIf - can add AElse");
                            }
                            // Case 4: The last child is an AElse containing an AIf (else-if chain continuation)
                            else if ((lastChild is AElse))
                            {
                                ScriptRootNode lastChildRoot = (ScriptRootNode)lastChild;
                                if (lastChildRoot.HasChildren())
                                {
                                    ScriptDomNode lastGrandChild = lastChildRoot.GetLastChild();
                                    if ((lastGrandChild is AIf))
                                    {
                                        canAddElse = true;
                                        SubScriptLogger.Trace("checkEnd: last child is AElse containing AIf - can add AElse");
                                    }
                                }
                            }

                            if (!canAddElse)
                            {
                                SubScriptLogger.Trace("checkEnd: Cannot add AElse to elseParent - no valid predecessor. lastChild=" +
                                      (lastChild != null ? lastChild.GetType().Name : "null") +
                                      ", current=" + current.GetType().Name +
                                      ", parent=" + (parent != null ? parent.GetType().Name : "null"));
                            }
                        }

                        // If we can't add the AElse to elseParent, fall back to using the direct parent
                        // This ensures the AElse will be added as a sibling of the AIf
                        if (!canAddElse)
                        {
                            if (elseParent != parent)
                            {
                                SubScriptLogger.Trace("checkEnd: Falling back to using direct parent for AElse");
                                elseParent = parent;
                                // Re-validate with the direct parent
                                if (elseParent.HasChildren() && elseParent.GetLastChild() == current)
                                {
                                    canAddElse = true;
                                    SubScriptLogger.Trace("checkEnd: AIf is last child of direct parent - can add AElse");
                                }
                                else
                                {
                                    SubScriptLogger.Trace("checkEnd: WARNING - AIf is not last child of direct parent either!");
                                    // This should not happen, but if it does, we'll still try to add the AElse
                                    // The structure might be malformed, but we can't fix it here
                                }
                            }
                            else
                            {
                                SubScriptLogger.Trace("checkEnd: WARNING - Cannot add AElse even to direct parent!");
                            }
                        }

                        int elseStart = current.GetEnd() + 6;
                        int elseEnd = nodedata.GetPos(NodeUtils.GetPreviousCommand(dest, nodedata));
                        SubScriptLogger.Trace("checkEnd: creating AElse start=" + elseStart + ", end=" + elseEnd +
                              ", elseParent=" + elseParent.GetType().Name);

                        AElse aelse = new AElse(elseStart, elseEnd);
                        current = elseParent;
                        current.AddChild(aelse);
                        current = aelse;
                        aelse = null;
                        dest = null;
                        return;
                    }
                    else
                    {
                        SubScriptLogger.Trace("checkEnd: destPos == aifEnd+6, no else block");
                    }
                }

                if ((current is ADoLoop))
                {
                    SubScriptLogger.Trace("checkEnd: current is ADoLoop, calling transformEndDoLoop");
                    TransformEndDoLoop();
                }

                ScriptRootNode newCurrent = (ScriptRootNode)current.Parent();
                SubScriptLogger.Trace("checkEnd: moving up to parent=" + (newCurrent != null ? newCurrent.GetType().Name : "null"));
                current = newCurrent;
            }

            SubScriptLogger.Trace("checkEnd: END, setting state=-1");
            state = StateDone;
        }

        public bool InActionArg()
        {
            return state == 2;
        }

        public void TransformPlaceholderVariableRemoved(Variable var)
        {
            AVarDecl vardec = (AVarDecl)vardecs[var];
            if (vardec != null && vardec.IsFcnReturn())
            {
                IAExpression exp = vardec.Exp();
                ScriptRootNode phParent = (ScriptRootNode)vardec.Parent();
                if (exp != null)
                {
                    phParent.ReplaceChild(vardec, (ScriptDomNode)exp);
                }
                else
                {
                    phParent.RemoveChild(vardec);
                }

                vardecs.Remove(var);
            }
        }

        public void EmitError(AstNode node, int pos)
        {
            string message = "ERROR: failed to decompile statement";
            if (pos >= 0)
            {
                message = message + " at " + pos;
            }

            current.AddChild(new AErrorComment(message));
        }

        private bool RemovingSwitchVar(List<Variable> vars, AstNode node)
        {
            if (vars.Count == 1 && current.HasChildren() && current.GetLastChild() is ASwitch)
            {
                IAExpression exp = ((ASwitch)current.GetLastChild()).SwitchExp();
                return (exp is AVarRef) && ReferenceEquals(((AVarRef)exp).Var(), vars[0]);
            }
            else
            {
                return false;
            }
        }

        public void TransformMoveSPVariablesRemoved(List<Variable> vars, AstNode node)
        {
            if (!AtLastCommand(node) || !CurrentContainsVars(vars))
            {
                if (vars.Count != 0)
                {
                    if (!IsMiddleOfReturn(node))
                    {
                        if (!RemovingSwitchVar(vars, node))
                        {
                            if (CurrentContainsVars(vars))
                            {
                                int earliestdec = -1;

                                foreach (Variable mv in vars)
                                {
                                    AVarDecl mvdec = (AVarDecl)vardecs[mv];
                                    earliestdec = GetEarlierDec(mvdec, earliestdec);
                                }

                                if (earliestdec != -1)
                                {
                                    AstNode prev = NodeUtils.GetPreviousCommand(node, nodedata);
                                    ACodeBlock block = new ACodeBlock(-1, nodedata.GetPos(prev));
                                    List<ScriptDomNode> children = current.RemoveChildren(earliestdec);
                                    current.AddChild(block);
                                    block.AddChildren(children);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void TransformEndDoLoop()
        {
            ((ADoLoop)current).Condition(RemoveLastExp(false));
        }

        public void TransformOriginFound(AstNode destination, AstNode origin)
        {
            AControlLoop loop = GetLoop(destination, origin);
            current.AddChild(loop);
            current = loop;
            if ((loop is AWhileLoop))
            {
                state = 3;
            }

            loop = null;
        }

        public void TransformLogOrExtraJump(AConditionalJumpCommand node)
        {
            RemoveLastExp(true);
        }

        public void TransformConditionalJump(AConditionalJumpCommand node)
        {
            CheckStart(node);
            if (state == 3)
            {
                ((AWhileLoop)current).Condition(RemoveLastExp(false));
                state = 0;
            }
            else if (!NodeUtils.IsJz(node))
            {
                // Equality comparison - prefer switch when preferSwitches is enabled
                if (state != 4)
                {
                    AConditionalExp cond = (AConditionalExp)RemoveLastExp(true);
                    // When preferSwitches is enabled, be more aggressive about creating switches
                    // Check if we can add to an existing switch or create a new one
                    bool canCreateSwitch = cond.Right() is AConst;
                    ASwitch existingSwitch = null;

                    // Check if we can continue an existing switch when preferSwitches is enabled
                    if (preferSwitches && current.HasChildren())
                    {
                        ScriptDomNode last = current.GetLastChild();
                        if (last is ASwitch)
                        {
                            existingSwitch = (ASwitch)last;
                            // Verify the switch expression matches
                            {
                                AVarRef leftRef = cond.Left() as AVarRef;
                                AVarRef swExp = existingSwitch.SwitchExp() as AVarRef;
                                if (leftRef != null && swExp != null && ReferenceEquals(leftRef.Var(), swExp.Var()))
                                {
                                    // Can continue existing switch
                                    ASwitchCase aprevcase = existingSwitch.GetLastCase();
                                    if (aprevcase != null)
                                    {
                                        aprevcase.End(nodedata
                                              .GetPos(NodeUtils.GetPreviousCommand(nodedata.GetDestination(node), nodedata)));
                                    }
                                    ASwitchCase acasex = new ASwitchCase(nodedata.GetPos(nodedata.GetDestination(node)),
                                          (AConst)cond.Right());
                                    existingSwitch.AddCase(acasex);
                                    state = 4;
                                    CheckEnd(node);
                                    return;
                                }
                            }
                        }
                    }

                    if (canCreateSwitch)
                    {
                        ASwitch aswitch = null;
                        ASwitchCase acase = new ASwitchCase(nodedata.GetPos(nodedata.GetDestination(node)),
                              (AConst)cond.Right());
                        if (current.HasChildren())
                        {
                            ScriptDomNode last = current.GetLastChild();
                            {
                                AVarRef lastVr = last as AVarRef;
                                AVarRef leftVr = cond.Left() as AVarRef;
                                if (lastVr != null && leftVr != null && ReferenceEquals(lastVr.Var(), leftVr.Var()))
                                {
                                    AVarRef varref = (AVarRef)RemoveLastExp(false);
                                    aswitch = new ASwitch(nodedata.GetPos(node), varref);
                                }
                            }
                        }

                        if (aswitch == null)
                        {
                            aswitch = new ASwitch(nodedata.GetPos(node), cond.Left());
                        }

                        current.AddChild(aswitch);
                        aswitch.AddCase(acase);
                        state = 4;
                    }
                    else
                    {
                        // Fall back to if statement if we can't create a switch
                        AIf aif = new AIf(nodedata.GetPos(node), nodedata.GetPos(nodedata.GetDestination(node)) - 6,
                              cond);
                        current.AddChild(aif);
                        current = aif;
                    }
                }
                else
                {
                    AConditionalExp condx = (AConditionalExp)RemoveLastExp(true);
                    ASwitch aswitchx = (ASwitch)current.GetLastChild();
                    ASwitchCase aprevcase = aswitchx.GetLastCase();
                    aprevcase.End(nodedata
                          .GetPos(NodeUtils.GetPreviousCommand(nodedata.GetDestination(node), nodedata)));
                    ASwitchCase acasex = new ASwitchCase(nodedata.GetPos(nodedata.GetDestination(node)),
                          (AConst)condx.Right());
                    aswitchx.AddCase(acasex);
                }
            }
            else if ((current is AIf) && IsModifyConditional() && state != 4)
            {
                // Don't modify AIf's end when processing switch cases (state == 4)
                int newEnd = nodedata.GetPos(nodedata.GetDestination(node)) - 6;
                SubScriptLogger.Trace("transformJZ: modifying AIf end (isModifyConditional) from " +
                      current.GetEnd() + " to " + newEnd);
                ((AIf)current).End(newEnd);
                if (current.HasChildren())
                {
                    current.RemoveLastChild();
                }
            }
            else if ((current is AIf) && IsModifyConditional() && state == 4)
            {
                SubScriptLogger.Trace("transformJZ: NOT modifying AIf end (state==4, processing switch case)");
            }
            else if ((current is AWhileLoop) && IsModifyConditional())
            {
                ((AWhileLoop)current).End(nodedata.GetPos(nodedata.GetDestination(node)) - 6);
                if (current.HasChildren())
                {
                    current.RemoveLastChild();
                }
            }
            else
            {
                AIf aif = new AIf(nodedata.GetPos(node), nodedata.GetPos(nodedata.GetDestination(node)) - 6,
                      RemoveLastExp(false));
                current.AddChild(aif);
                current = aif;
            }

            // Ensure AIf's end is up-to-date before checkEnd, in case it needs to check for else blocks
            // The end might have been set in the constructor, but we should verify it's correct
            // BUT: Don't update AIf's end when we're processing switch cases (state == 4), as those
            // JZ instructions are for switch case checks, not the original if condition
            if ((current is AIf) && state != 4)
            {
                AstNode destNode = nodedata.GetDestination(node);
                if (destNode != null)
                {
                    int expectedEnd = nodedata.GetPos(destNode) - 6;
                    int currentEnd = current.GetEnd();
                    SubScriptLogger.Trace("transformJZ: AIf end check - currentEnd=" + currentEnd +
                          ", expectedEnd=" + expectedEnd + ", nodePos=" + nodedata.GetPos(node));
                    if (currentEnd != expectedEnd)
                    {
                        SubScriptLogger.Trace("transformJZ: UPDATING AIf end from " + currentEnd + " to " + expectedEnd);
                        ((AIf)current).End(expectedEnd);
                    }
                    else
                    {
                        SubScriptLogger.Trace("transformJZ: AIf end already correct, not updating");
                    }
                }
            }
            else if ((current is AIf) && state == 4)
            {
                SubScriptLogger.Trace("transformJZ: AIf end NOT updated (state==4, processing switch case)");
            }

            CheckEnd(node);
        }

        private bool IsModifyConditional()
        {
            if (!current.HasChildren())
            {
                return true;
            }
            else if (current.Size() == 1)
            {
                ScriptDomNode last = current.GetLastChild();
                return (last is AVarRef) && !((AVarRef)last).Var().IsAssigned()
                      && !((AVarRef)last).Var().IsParam();
            }
            else
            {
                return false;
            }
        }

        public void TransformJump(AJumpCommand node)
        {
            CheckStart(node);
            AstNode dest = nodedata.GetDestination(node);
            int nodePos = nodedata.GetPos(node);
            int destPos = dest != null ? nodedata.GetPos(dest) : -1;

            SubScriptLogger.Trace("transformJump: pos=" + nodePos + " (0x" + nodePos.ToString("X") + "), destPos=" + destPos +
                  ", state=" + state + ", current=" + current.GetType().Name +
                  ", currentEnd=" + current.GetEnd() + ", destType=" + (dest != null ? dest.GetType().Name : "null"));

            if (state == 2)
            {
                SubScriptLogger.Trace("transformJump: state==2, creating AActionArgExp");
                state = 0;
                int start = GetNextCommand(node);
                int end = GetPriorToDestCommand(node);
                // Prefer using the actual previous command node to compute the end of the action-arg block.
                // destPos-2 is not reliable across encodings and can truncate the block, causing the
                // action argument statements to be emitted into the parent scope (breaking AssignCommand/DelayCommand).
                if (dest != null)
                {
                    AstNode prev = NodeUtils.GetPreviousCommand(dest, nodedata);
                    if (prev != null)
                    {
                        end = nodedata.GetPos(prev);
                    }
                }
                AActionArgExp aarg = new AActionArgExp(start, end);
                current.AddChild(aarg);
                current = aarg;
            }
            else
            {
                bool atIfEnd = IsAtIfEnd(node);
                SubScriptLogger.Trace("transformJump: isAtIfEnd=" + atIfEnd);

                if (!atIfEnd)
                {
                    // Only process as return/break/continue if we're NOT at the end of an enclosing AIf
                    // (otherwise, this JMP is the "skip else" jump and should be handled by checkEnd)
                    if (state == 4)
                    {
                        SubScriptLogger.Trace("transformJump: state==4 (switch), handling switch case/end");
                        ASwitch aswitch = (ASwitch)current.GetLastChild();
                        ASwitchCase aprevcase = aswitch.GetLastCase();
                        if (aprevcase != null)
                        {
                            int prevCaseEnd = nodedata.GetPos(NodeUtils.GetPreviousCommand(dest, nodedata));
                            SubScriptLogger.Trace("transformJump: setting prevCase end to " + prevCaseEnd);
                            aprevcase.End(prevCaseEnd);
                        }

                        if ((dest is AMoveSpCommand))
                        {
                            int switchEnd = nodedata.GetPos(nodedata.GetDestination(node));
                            SubScriptLogger.Trace("transformJump: dest is MoveSpCommand, setting switch end to " + switchEnd);
                            aswitch.End(switchEnd);
                        }
                        else
                        {
                            int defaultStart = nodedata.GetPos(dest);
                            SubScriptLogger.Trace("transformJump: creating default case at " + defaultStart);
                            ASwitchCase adefault = new ASwitchCase(defaultStart);
                            aswitch.AddDefaultCase(adefault);
                        }

                        state = 0;
                    }
                    else
                    {
                        bool isRet = IsReturn(node);
                        SubScriptLogger.Trace("transformJump: isReturn=" + isRet);

                        if (isRet)
                        {
                            SubScriptLogger.Trace("transformJump: treating as RETURN, adding AReturnStatement to " + current.GetType().Name);
                            AReturnStatement areturn;
                            if (!root.ReturnType().Equals((byte)0))
                            {
                                areturn = new AReturnStatement(GetReturnExp());
                            }
                            else
                            {
                                areturn = new AReturnStatement();
                            }

                            // If we're inside a switch case, ensure the return is added to the case, not the parent
                            // The return JMP might be at the end of a switch case, but checkEnd from the previous
                            // instruction may have already moved this.current up. We need to find the switch case
                            // that ends at nodePos (or just before it) and add the return to that case.
                            ScriptRootNode targetNode = current;
                            ASwitchCase switchCase = null;

                            // First check if current is a switch case
                            if ((current is ASwitchCase))
                            {
                                SubScriptLogger.Trace("transformJump: current is ASwitchCase, adding return to case");
                                targetNode = current;
                                switchCase = (ASwitchCase)current;
                            }
                            else
                            {
                                // Walk up the parent chain to find a switch case
                                ScriptRootNode curr = current;
                                while (curr != null && !(curr is ASub))
                                {
                                    if ((curr is ASwitchCase))
                                    {
                                        SubScriptLogger.Trace("transformJump: found ASwitchCase in parent chain");
                                        switchCase = (ASwitchCase)curr;
                                        break;
                                    }
                                    ScriptDomNode parent = curr.Parent();
                                    curr = parent as ScriptRootNode;
                                }

                                // If we found a switch case, check if nodePos is at or just after its end
                                // (the return JMP is typically the last instruction in a case)
                                if (switchCase != null)
                                {
                                    int caseEnd = switchCase.GetEnd();
                                    SubScriptLogger.Trace("transformJump: switchCase end=" + caseEnd + ", nodePos=" + nodePos);
                                    // The return JMP is typically at the end of the case, or the case end might be
                                    // set to the position just before the return. Check if nodePos matches caseEnd
                                    // or if caseEnd is just before nodePos (within a few bytes).
                                    if (nodePos == caseEnd || (caseEnd > 0 && nodePos >= caseEnd - 6 && nodePos <= caseEnd + 6))
                                    {
                                        SubScriptLogger.Trace("transformJump: nodePos matches switchCase end, adding return to case");
                                        targetNode = switchCase;
                                    }
                                    else
                                    {
                                        SubScriptLogger.Trace("transformJump: nodePos does not match switchCase end, using current");
                                    }
                                }
                                else
                                {
                                    // Check if there's a switch in the children that has a case ending at nodePos
                                    if (current.HasChildren())
                                    {
                                        ScriptDomNode lastChild = current.GetLastChild();
                                        if ((lastChild is ASwitch))
                                        {
                                            ASwitch aswitch = (ASwitch)lastChild;
                                            // Check all cases to see if any end at nodePos
                                            ASwitchCase acase = null;
                                            while ((acase = aswitch.GetNextCase(acase)) != null)
                                            {
                                                int caseEnd = acase.GetEnd();
                                                SubScriptLogger.Trace("transformJump: checking case end=" + caseEnd + " vs nodePos=" + nodePos);
                                                if (nodePos == caseEnd || (caseEnd > 0 && nodePos >= caseEnd - 6 && nodePos <= caseEnd + 6))
                                                {
                                                    SubScriptLogger.Trace("transformJump: found case ending at nodePos, adding return to case");
                                                    targetNode = acase;
                                                    break;
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                            targetNode.AddChild(areturn);
                        }
                        else if (destPos >= nodePos)
                        {
                            SubScriptLogger.Trace("transformJump: forward jump (destPos >= nodePos), checking for break/continue");
                            ScriptRootNode loop = GetBreakable();
                            SubScriptLogger.Trace("transformJump: breakable=" + (loop != null ? loop.GetType().Name : "null"));

                            if ((loop is ASwitchCase))
                            {
                                loop = GetEnclosingLoop(loop);
                                SubScriptLogger.Trace("transformJump: enclosingLoop=" + (loop != null ? loop.GetType().Name : "null"));
                                if (loop == null)
                                {
                                    SubScriptLogger.Trace("transformJump: adding ABreakStatement (no enclosing loop)");
                                    ABreakStatement abreak = new ABreakStatement();
                                    current.AddChild(abreak);
                                }
                                else
                                {
                                    SubScriptLogger.Trace("transformJump: adding AUnkLoopControl");
                                    AUnkLoopControl aunk = new AUnkLoopControl(destPos);
                                    current.AddChild(aunk);
                                }
                            }
                            else if (loop != null && destPos > loop.GetEnd())
                            {
                                SubScriptLogger.Trace("transformJump: adding ABreakStatement (destPos > loop.end)");
                                ABreakStatement abreak = new ABreakStatement();
                                current.AddChild(abreak);
                            }
                            else
                            {
                                loop = GetLoop();
                                SubScriptLogger.Trace("transformJump: getLoop()=" + (loop != null ? loop.GetType().Name : "null"));
                                if (loop != null && destPos <= loop.GetEnd())
                                {
                                    SubScriptLogger.Trace("transformJump: adding AContinueStatement");
                                    AContinueStatement acont = new AContinueStatement();
                                    current.AddChild(acont);
                                }
                            }
                        }
                        else
                        {
                            SubScriptLogger.Trace("transformJump: backward jump, no action taken");
                        }
                    }
                }
                else
                {
                    SubScriptLogger.Trace("transformJump: at if end, skipping return/break/continue handling (will be handled by checkEnd)");
                }
            }

            SubScriptLogger.Trace("transformJump: calling checkEnd, current=" + current.GetType().Name);
            CheckEnd(node);
        }

        public void TransformJSR(AJumpToSubroutine node)
        {
            CheckStart(node);
            AFcnCallExp jsr = new AFcnCallExp(GetFcnId(node), RemoveFcnParams(node));
            if (!GetFcnType(node).Equals((byte)0))
            {
                // Ensure there's a decl to attach; if none, create a placeholder
                Variable retVar = stack.Size() >= 1 ? (Variable)stack.Get(1) : new Variable(new DecompType((byte)0));
                AVarDecl decl;
                // Check if variable is already declared to prevent duplicates
                decl = (AVarDecl)vardecs[retVar];
                if (decl == null)
                {
                    // Also check if last child is a matching AVarDecl
                    if (current.HasChildren() && current.GetLastChild() is AVarDecl)
                    {
                        AVarDecl lastDecl = (AVarDecl)current.GetLastChild();
                        if (lastDecl.Var() == retVar)
                        {
                            decl = lastDecl;
                            vardecs[retVar] = decl;
                        }
                    }
                    if (decl == null)
                    {
                        decl = new AVarDecl(retVar);
                        UpdateVarCount(retVar);
                        current.AddChild(decl);
                        vardecs[retVar] = decl;
                    }
                }
                decl.IsFcnReturn(true);
                decl.InitializeExp(jsr);
                jsr.Stackentry(retVar);
            }
            else
            {
                current.AddChild(jsr);
            }

            CheckEnd(node);
        }

        public void TransformAction(AActionCommand node)
        {
            CheckStart(node);
            List<IAExpression> fparams = RemoveActionParams(node);
            string actionName;
            try
            {
                actionName = NodeUtils.GetActionName(node, actions);
            }
            catch (Exception)
            {
                // Action metadata missing - use placeholder name
                actionName = "UnknownAction" + NodeUtils.GetActionId(node);
            }
            AActionExp act = new AActionExp(actionName, NodeUtils.GetActionId(node), fparams, actions);
            DecompType type;
            try
            {
                type = NodeUtils.GetReturnType(node, actions);
            }
            catch (Exception)
            {
                // Action metadata missing or invalid - assume void return
                type = new DecompType((byte)0);
            }
            if (!type.Equals((byte)0))
            {
                Variable var = (Variable)stack.Get(1);
                if (type.Equals(DecompType.VtVector))
                {
                    var = var.Varstruct();
                }

                act.Stackentry(var);
                // Check if variable is already declared to prevent duplicates
                AVarDecl vardec = (AVarDecl)vardecs[var];
                if (vardec == null)
                {
                    vardec = new AVarDecl(var);
                    UpdateVarCount(var);
                    current.AddChild(vardec);
                    vardecs[var] = vardec;
                }
                vardec.IsFcnReturn(true);
                vardec.InitializeExp(act);
            }
            else
            {
                current.AddChild(act);
            }

            CheckEnd(node);
        }

        public void TransformReturn(AReturn node)
        {
            CheckStart(node);
            CheckEnd(node);
        }

        public void TransformCopyDownSp(ACopyDownSpCommand node)
        {
            CheckStart(node);
            int nodePos = nodedata.GetPos(node);
            bool isRet = IsReturn(node);
            SubScriptLogger.Trace("transformCopyDownSp: pos=" + nodePos + ", isReturn=" + isRet +
                  ", current=" + current.GetType().Name +
                  ", hasChildren=" + current.HasChildren());

            IAExpression exp = RemoveLastExp(false);
            SubScriptLogger.Trace("transformCopyDownSp: extracted exp=" +
                  (exp != null ? exp.GetType().Name : "null") +
                  ", current hasChildren=" + current.HasChildren());

            if (isRet)
            {
                SubScriptLogger.Trace("transformCopyDownSp: creating AReturnStatement");
                AReturnStatement ret = new AReturnStatement(exp);
                current.AddChild(ret);
            }
            else
            {
                AVarRef varref = GetVarToAssignTo(node);
                AModifyExp modexp = new AModifyExp(varref, exp);
                UpdateName(varref, exp);
                current.AddChild(modexp);
                state = 1;
            }

            CheckEnd(node);
        }

        private void UpdateName(AVarRef varref, IAExpression exp)
        {
            if ((exp is AActionExp))
            {
                string name = NameGenerator.GetNameFromAction((AActionExp)exp);
                if (name != null && !varnames.ContainsKey(name))
                {
                    varref.Var().Name(name);
                    varnames[name] = 1;
                }
            }
        }

        public void TransformCopyTopSp(ACopyTopSpCommand node)
        {
            CheckStart(node);
            int nodePos = nodedata.GetPos(node);

            if (state == 5)
            {
                state = 0;
            }
            else
            {
                int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
                StackEntry sourceEntry = stack.Get(loc);
                SubScriptLogger.Trace("transformCopyTopSp: pos=" + nodePos + ", loc=" + loc +
                      ", sourceEntry=" + (sourceEntry != null ? sourceEntry.GetType().Name : "null") +
                      ", state=" + state + ", current=" + current.GetType().Name +
                      ", hasChildren=" + current.HasChildren());

                // For constants: when copying a constant that's already the last child,
                // we're likely doing short-circuit evaluation (e.g., for || or &&).
                // In this case, don't add a duplicate child - the duplicate is only
                // needed for stack simulation (the JZ/JNZ check), not for AST building.
                // The LOGORII/LOGANDII will consume the original constant from children,
                // leaving the correct expression (e.g., function result) for EQUAL.
                if ((sourceEntry is Const) && current.HasChildren())
                {
                    ScriptDomNode last = current.GetLastChild();
                    if ((last is AConst))
                    {
                        AConst lastConst = (AConst)last;
                        // Check if we're copying the exact same Const object
                        // stackentry() returns the Const for AConst nodes
                        if (lastConst.Stackentry() == sourceEntry)
                        {
                            // Short-circuit pattern: don't add duplicate to children
                            SubScriptLogger.Trace("transformCopyTopSp: skipping duplicate constant (short-circuit)");
                            CheckEnd(node);
                            return;
                        }
                    }
                }

                // For variables: similar logic - if we're copying a variable that's already the last child,
                // and it's the same variable, we might be doing short-circuit evaluation.
                // Don't add a duplicate AVarRef in this case.
                if ((sourceEntry is Variable) && current.HasChildren())
                {
                    ScriptDomNode last = current.GetLastChild();
                    if ((last is AVarRef))
                    {
                        AVarRef lastVarRef = (AVarRef)last;
                        if (lastVarRef.Var() == sourceEntry)
                        {
                            // Short-circuit pattern: don't add duplicate to children
                            Variable var = (Variable)sourceEntry;
                            SubScriptLogger.Trace("transformCopyTopSp: skipping duplicate AVarRef (short-circuit), var=" + var);
                            CheckEnd(node);
                            return;
                        }
                    }
                }

                IAExpression varref = GetVarToCopy(node);
                string varName = varref is AVarRef vref ? vref.Var().ToString() : "N/A";
                SubScriptLogger.Trace("transformCopyTopSp: adding " + varref.GetType().Name +
                      " to AST, var=" + varName);
                current.AddChild((ScriptDomNode)varref);
            }

            CheckEnd(node);
        }

        public void TransformCopyDownBp(ACopyDownBpCommand node)
        {
            CheckStart(node);
            AVarRef varref = GetVarToAssignTo(node);
            IAExpression exp = RemoveLastExp(false);
            AModifyExp modexp = new AModifyExp(varref, exp);
            current.AddChild(modexp);
            state = 1;
            CheckEnd(node);
        }

        public void TransformCopyTopBp(ACopyTopBpCommand node)
        {
            CheckStart(node);
            IAExpression varref = GetVarToCopy(node);
            current.AddChild((ScriptDomNode)varref);
            CheckEnd(node);
        }

        public void TransformMoveSp(AMoveSpCommand node)
        {
            CheckStart(node);
            int nodePos = nodedata.GetPos(node);
            SubScriptLogger.Trace("transformMoveSp: pos=" + nodePos + ", state=" + state +
                  ", current=" + current.GetType().Name);

            if (state == 1)
            {
                ScriptDomNode last = current.HasChildren() ? current.GetLastChild() : null;
                SubScriptLogger.Trace("transformMoveSp: state==1, last=" +
                      (last != null ? last.GetType().Name : "null"));

                if (!(last is AReturnStatement))
                {
                    IAExpression expr = null;
                    if ((last is AModifyExp))
                    {
                        SubScriptLogger.Trace("transformMoveSp: last is AModifyExp, removing as expression");
                        expr = (AModifyExp)RemoveLastExp(true);
                    }
                    else if ((last is AVarDecl) && ((AVarDecl)last).IsFcnReturn() && ((AVarDecl)last).Exp() != null)
                    {
                        SubScriptLogger.Trace("transformMoveSp: last is AVarDecl with function return");
                        // Function return value - extract the expression and convert to statement
                        // However, don't extract function calls (AActionExp) as standalone statements
                        // when in assignment context, as they're almost always part of a larger expression
                        // (e.g., GetGlobalNumber("X") == value, or function calls in binary operations).
                        IAExpression funcExp = ((AVarDecl)last).Exp();
                        if ((funcExp is AActionExp))
                        {
                            // Don't extract function calls as statements in assignment context
                            // They're almost always part of a larger expression being built
                            // Leave the AVarDecl in place - it will be used by EQUAL/other operations
                            // NEVER extract function calls as statements when state == 1 (assignment context)
                            SubScriptLogger.Trace("transformMoveSp: function call, NOT extracting as statement");
                            expr = null; // Don't extract as statement
                        }
                        else
                        {
                            // Non-function-call expressions can be extracted
                            SubScriptLogger.Trace("transformMoveSp: extracting expression from AVarDecl");
                            expr = ((AVarDecl)last).RemoveExp();
                            current.RemoveLastChild(); // Remove the AVarDecl
                        }
                    }
                    else if ((last is AUnaryModExp) || (last is IAExpression))
                    {
                        SubScriptLogger.Trace("transformMoveSp: last is AUnaryModExp or IAExpression, removing as expression");
                        // Gracefully handle postfix/prefix inc/dec and other loose expressions.
                        // However, don't extract function calls (AActionExp) as standalone statements
                        // when in assignment context, as they're almost always part of a larger expression
                        // (e.g., GetGlobalNumber("X") == value, or function calls in binary operations).
                        // In assignment context, function calls should remain as part of the expression tree
                        // until the full expression is built (e.g., by EQUAL, ADD, etc. operations).
                        expr = (IAExpression)RemoveLastExp(true);
                        SubScriptLogger.Trace("transformMoveSp: removed expression=" +
                              (expr != null ? expr.GetType().Name : "null"));
                        // Don't extract function calls as statements in assignment context
                        // They're almost always part of a larger expression being built.
                        // In assignment context (state == 1), function calls should remain as part of the expression tree
                        // until the full expression is built (e.g., by EQUAL, ADD, etc. operations).
                        if ((expr is AActionExp))
                        {
                            // Put the function call back - it's part of a larger expression
                            // Function calls in assignment context are almost never standalone statements
                            SubScriptLogger.Trace("transformMoveSp: function call, putting back");
                            current.AddChild((ScriptDomNode)expr);
                            expr = null; // Don't extract as statement
                        }
                    }
                    else
                    {
                        SubScriptLogger.Trace("transformMoveSp: WARNING - unexpected last child type: " +
                              (last != null ? last.GetType().Name : "null") + " at " + nodePos);
                        SubScriptLogger.Trace("uh-oh... not a modify exp at " + nodePos + ", " + last);
                    }

                    if (expr != null)
                    {
                        SubScriptLogger.Trace("transformMoveSp: creating AExpressionStatement with " + expr.GetType().Name);
                        AExpressionStatement stmt = new AExpressionStatement(expr);
                        current.AddChild(stmt);
                        stmt.Parent(current);
                    }
                    else
                    {
                        SubScriptLogger.Trace("transformMoveSp: NOT creating AExpressionStatement (expr is null)");
                    }
                }
                else
                {
                    SubScriptLogger.Trace("transformMoveSp: last is AReturnStatement, skipping expression statement creation");
                }

                state = 0;
            }
            else
            {
                SubScriptLogger.Trace("transformMoveSp: state != 1, checking for standalone expression statement");
                // When state == 0, check if we have a standalone expression (like int3;)
                // that should be converted to an expression statement
                if (current.HasChildren())
                {
                    ScriptDomNode last = current.GetLastChild();
                    // If the last child is a plain expression (AVarRef, AConst, etc.) that's not part of
                    // a larger expression, convert it to an expression statement
                    // But don't do this for function calls (AActionExp) as they're usually part of expressions
                    if ((last is IAExpression) && !(last is AActionExp)
                          && !(last is AModifyExp) && !(last is AUnaryModExp)
                          && !(last is AReturnStatement))
                    {
                        SubScriptLogger.Trace("transformMoveSp: converting standalone expression to statement: " +
                              last.GetType().Name);
                        IAExpression expr = (IAExpression)RemoveLastExp(true);
                        if (expr != null)
                        {
                            AExpressionStatement stmt = new AExpressionStatement(expr);
                            current.AddChild(stmt);
                            stmt.Parent(current);
                            SubScriptLogger.Trace("transformMoveSp: created AExpressionStatement");
                        }
                    }
                    else
                    {
                        SubScriptLogger.Trace("transformMoveSp: last child is not a standalone expression, calling checkSwitchEnd");
                        CheckSwitchEnd(node);
                    }
                }
                else
                {
                    SubScriptLogger.Trace("transformMoveSp: no children, calling checkSwitchEnd");
                    CheckSwitchEnd(node);
                }
            }

            CheckEnd(node);
        }

        public void TransformRSAdd(ARsaddCommand node)
        {
            CheckStart(node);
            Variable var = (Variable)stack.Get(1);
            // Check if variable is already declared to prevent duplicates
            AVarDecl existingVardec = (AVarDecl)vardecs[var];
            if (existingVardec == null)
            {
                AVarDecl vardec = new AVarDecl(var);
                UpdateVarCount(var);
                current.AddChild(vardec);
                vardecs[var] = vardec;
            }
            CheckEnd(node);
        }

        public void TransformConst(AConstCommand node)
        {
            CheckStart(node);
            Const theconst = (Const)stack.Get(1);
            AConst constdec = new AConst(theconst);
            current.AddChild(constdec);
            CheckEnd(node);
        }

        public void TransformLogii(ALogiiCommand node)
        {
            CheckStart(node);
            string op = NodeUtils.GetOp(node);
            if (!current.HasChildren() && current is AIf
                  && current.Parent() is AIf)
            {
                AIf right = (AIf)current;
                AIf left = (AIf)current.Parent();
                IAExpression leftCond = left.Condition();
                IAExpression rightCond = right.Condition();
                // For bytecode-perfect round-tripping: mark sub-expressions for explicit grouping
                MarkForGroupingIfNeeded(leftCond, op);
                MarkForGroupingIfNeeded(rightCond, op);
                AConditionalExp conexp = new AConditionalExp(leftCond, rightCond, op);
                conexp.Stackentry(stack.Get(1));
                current = (ScriptRootNode)current.Parent();
                ((AIf)current).Condition(conexp);
                current.RemoveLastChild();
            }
            else
            {
                IAExpression right = RemoveLastExp(false);
                if (!current.HasChildren() && (current is AIf))
                {
                    IAExpression left = ((AIf)current).Condition();
                    // For bytecode-perfect round-tripping: mark sub-expressions for explicit grouping
                    MarkForGroupingIfNeeded(left, op);
                    MarkForGroupingIfNeeded(right, op);
                    AConditionalExp conexp = new AConditionalExp(left, right, op);
                    conexp.Stackentry(stack.Get(1));
                    ((AIf)current).Condition(conexp);
                }
                else if (!current.HasChildren() && (current is AWhileLoop))
                {
                    IAExpression left = ((AWhileLoop)current).Condition();
                    // For bytecode-perfect round-tripping: mark sub-expressions for explicit grouping
                    MarkForGroupingIfNeeded(left, op);
                    MarkForGroupingIfNeeded(right, op);
                    AConditionalExp conexp = new AConditionalExp(left, right, op);
                    conexp.Stackentry(stack.Get(1));
                    ((AWhileLoop)current).Condition(conexp);
                }
                else
                {
                    IAExpression left = RemoveLastExp(false);
                    // For bytecode-perfect round-tripping: mark sub-expressions for explicit grouping
                    MarkForGroupingIfNeeded(left, op);
                    MarkForGroupingIfNeeded(right, op);
                    AConditionalExp conexp = new AConditionalExp(left, right, op);
                    conexp.Stackentry(stack.Get(1));
                    current.AddChild(conexp);
                }
            }

            CheckEnd(node);
        }

        /**
         * For bytecode-perfect round-tripping: when combining expressions with && or ||,
         * mark sub-expressions that are themselves && or || conditionals with forceParens.
         * This ensures ((A && B) && (C && D)) round-trips correctly instead of becoming
         * A && B && C && D, which compiles to different bytecode.
         */
        private void MarkForGroupingIfNeeded(IAExpression expr, string parentOp)
        {
            if (expr is AConditionalExp condExpr)
            {
                string childOp = condExpr.Op();
                if (parentOp != null && parentOp.Equals(childOp))
                {
                    condExpr.ForceParens(true);
                }
            }
        }

        public void TransformBinary(ABinaryCommand node)
        {
            CheckStart(node);
            int nodePos = nodedata.GetPos(node);
            SubScriptLogger.Trace("transformBinary: pos=" + nodePos + ", op=" + NodeUtils.GetOp(node) +
                  ", state=" + state + ", current=" + current.GetType().Name +
                  ", hasChildren=" + current.HasChildren());

            IAExpression right = RemoveLastExp(false);
            SubScriptLogger.Trace("transformBinary: right=" + (right != null ? right.GetType().Name : "null"));

            IAExpression left = RemoveLastExp(state == 4);
            SubScriptLogger.Trace("transformBinary: left=" + (left != null ? left.GetType().Name : "null"));

            IAExpression exp;
            if (NodeUtils.IsArithmeticOp(node))
            {
                exp = new ABinaryExp(left, right, NodeUtils.GetOp(node));
            }
            else
            {
                if (!NodeUtils.IsConditionalOp(node))
                {
                    throw new InvalidOperationException("Unknown binary op at " + nodePos);
                }

                exp = new AConditionalExp(left, right, NodeUtils.GetOp(node));
            }

            exp.Stackentry(stack.Get(1));
            current.AddChild((ScriptDomNode)exp);
            SubScriptLogger.Trace("transformBinary: created " + exp.GetType().Name +
                  ", current hasChildren=" + current.HasChildren());
            CheckEnd(node);
        }

        public void TransformUnary(AUnaryCommand node)
        {
            CheckStart(node);
            IAExpression exp = RemoveLastExp(false);
            AUnaryExp unexp = new AUnaryExp(exp, NodeUtils.GetOp(node));
            unexp.Stackentry(stack.Get(1));
            current.AddChild(unexp);
            CheckEnd(node);
        }

        public void TransformStack(AStackCommand node)
        {
            CheckStart(node);
            ScriptDomNode last = current.GetLastChild();
            AVarRef varref = GetVarToAssignTo(node);
            bool prefix;
            if ((last is AVarRef) && ((AVarRef)last).Var() == varref.Var())
            {
                RemoveLastExp(true);
                prefix = false;
            }
            else
            {
                state = 5;
                prefix = true;
            }

            AUnaryModExp unexp = new AUnaryModExp(varref, NodeUtils.GetOp(node), prefix);
            unexp.Stackentry(stack.Get(1));
            current.AddChild(unexp);
            CheckEnd(node);
        }

        public void TransformDestruct(ADestructCommand node)
        {
            CheckStart(node);
            UpdateStructVar(node);
            CheckEnd(node);
        }

        public void TransformBp(ABpCommand node)
        {
            CheckStart(node);
            CheckEnd(node);
        }

        public void TransformStoreState(AStoreStateCommand node)
        {
            CheckStart(node);
            state = 2;
            CheckEnd(node);
        }

        public void TransformDeadCode(AstNode node)
        {
            CheckEnd(node);
        }

        /**
         * Checks if the current node position is at the end of any enclosing AIf.
         * This is used to detect "skip else" jumps that should not be treated as returns.
         */
        private bool IsAtIfEnd(AstNode node)
        {
            int nodePos = nodedata.GetPos(node);

            SubScriptLogger.Trace("isAtIfEnd: nodePos=" + nodePos + ", current=" + current.GetType().Name + ", currentEnd=" + current.GetEnd());

            if ((current is AIf) && nodePos == current.GetEnd())
            {
                SubScriptLogger.Trace("isAtIfEnd: returning true (current is AIf)");
                return true;
            }

            if ((current is ASwitchCase))
            {
                ScriptDomNode switchNode = current.Parent();
                SubScriptLogger.Trace("isAtIfEnd: in switch case, switchNode=" + (switchNode != null ? switchNode.GetType().Name : "null"));
                if ((switchNode is ASwitch))
                {
                    ScriptDomNode switchParent = switchNode.Parent();
                    SubScriptLogger.Trace("isAtIfEnd: switchParent=" + (switchParent != null ? switchParent.GetType().Name : "null"));
                    if (switchParent is AIf aifParent)
                    {
                        int parentEnd = aifParent.GetEnd();
                        SubScriptLogger.Trace("isAtIfEnd: parentEnd=" + parentEnd);
                        if (nodePos == parentEnd)
                        {
                            SubScriptLogger.Trace("isAtIfEnd: returning true (switch in AIf)");
                            return true;
                        }
                    }
                }
            }

            ScriptRootNode curr = current;
            while (curr != null && !(curr is ASub))
            {
                if ((curr is AIf) && nodePos == curr.GetEnd())
                {
                    SubScriptLogger.Trace("isAtIfEnd: returning true (found AIf in parent chain)");
                    return true;
                }
                ScriptDomNode parent = curr.Parent();
                curr = parent as ScriptRootNode;
            }

            SubScriptLogger.Trace("isAtIfEnd: returning false");
            return false;
        }

        public bool AtLastCommand(AstNode node)
        {
            if (nodedata.GetPos(node) == current.GetEnd())
            {
                return true;
            }
            else if ((current is ASwitchCase)
                  && ((ASwitch)((ASwitchCase)current).Parent()).End() == nodedata.GetPos(node))
            {
                return true;
            }
            else
            {
                if ((current is ASub))
                {
                    AstNode next = NodeUtils.GetNextCommand(node, nodedata);
                    if (next == null)
                    {
                        return true;
                    }
                }

                if ((current is AIf) || (current is AElse))
                {
                    AstNode next = NodeUtils.GetNextCommand(node, nodedata);
                    if (next != null && nodedata.GetPos(next) == current.GetEnd())
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool IsMiddleOfReturn(AstNode node)
        {
            if (!root.ReturnType().Equals((byte)0) && current.HasChildren()
                  && current.GetLastChild() is AReturnStatement)
            {
                return true;
            }
            else
            {
                if (root.ReturnType().Equals((byte)0))
                {
                    AstNode next = NodeUtils.GetNextCommand(node, nodedata);
                    if (next != null && next is AJumpCommand
                          && nodedata.GetDestination(next) is AReturn)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        public bool CurrentContainsVars(List<Variable> vars)
        {
            foreach (Variable var in vars)
            {
                if (!var.IsParam())
                {
                    AVarDecl vardec = (AVarDecl)vardecs[var];
                    if (vardec != null)
                    {
                        ScriptDomNode parent = vardec.Parent();
                        bool found = false;

                        while (parent != null && !found)
                        {
                            if (parent == current)
                            {
                                found = true;
                            }
                            else
                            {
                                parent = parent.Parent();
                            }
                        }

                        if (!found)
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private int GetEarlierDec(AVarDecl vardec, int earliestdec)
        {
            if (current.GetChildLocation(vardec) == -1)
            {
                return -1;
            }
            else if (earliestdec == -1)
            {
                return current.GetChildLocation(vardec);
            }
            else
            {
                return current.GetChildLocation(vardec) < earliestdec ? current.GetChildLocation(vardec)
                      : earliestdec;
            }
        }

        public IAExpression GetReturnExp()
        {
            SubScriptLogger.Trace("getReturnExp: current=" + current.GetType().Name +
                  ", hasChildren=" + current.HasChildren());

            if (!current.HasChildren())
            {
                SubScriptLogger.Trace("getReturnExp: no children, returning placeholder");
                return new AConst(new IntConst(0L));
            }

            ScriptDomNode last = current.RemoveLastChild();
            SubScriptLogger.Trace("getReturnExp: removed last child=" + last.GetType().Name);

            if ((last is AModifyExp))
            {
                SubScriptLogger.Trace("getReturnExp: last is AModifyExp, extracting expression");
                return ((AModifyExp)last).Expression();
            }
            else if ((last is AExpressionStatement))
            {
                IAExpression exp = ((AExpressionStatement)last).Exp();
                SubScriptLogger.Trace("getReturnExp: last is AExpressionStatement, exp=" +
                      (exp != null ? exp.GetType().Name : "null"));

                if ((exp is AModifyExp))
                {
                    SubScriptLogger.Trace("getReturnExp: extracting expression from AModifyExp inside AExpressionStatement");
                    return ((AModifyExp)exp).Expression();
                }
                else if ((exp is IAExpression))
                {
                    // AExpressionStatement containing a plain expression (e.g., AVarRef)
                    // Extract the expression for the return statement
                    // IMPORTANT: The AExpressionStatement has been removed from the AST, so the expression
                    // inside it should be extracted and used. However, we need to clear the parent relationship
                    // since the AExpressionStatement is being discarded.
                    SubScriptLogger.Trace("getReturnExp: extracting plain expression from AExpressionStatement");
                    exp.Parent(null); // Clear parent since AExpressionStatement is being discarded
                    return exp;
                }
                else
                {
                    SubScriptLogger.Trace("getReturnExp: AExpressionStatement with unexpected exp type, returning placeholder");
                    return new AConst(new IntConst(0L));
                }
            }
            else if ((last is AReturnStatement))
            {
                SubScriptLogger.Trace("getReturnExp: last is AReturnStatement, extracting exp");
                return ((AReturnStatement)last).Exp();
            }
            else if ((last is IAExpression))
            {
                SubScriptLogger.Trace("getReturnExp: last is IAExpression, returning directly");
                return (IAExpression)last;
            }
            else
            {
                // Keep decompilation alive; emit placeholder when structure is unexpected.
                SubScriptLogger.Trace("getReturnExp: unexpected last child type, returning placeholder");
                return new AConst(new IntConst(0L));
            }
        }

        private void CheckSwitchEnd(AMoveSpCommand node)
        {
            if ((current is ASwitchCase))
            {
                StackEntry entry = stack.Get(1);
                if ((entry is Variable)
                      && ((ASwitch)current.Parent()).SwitchExp().Stackentry().Equals(entry))
                {
                    ((ASwitch)current.Parent()).End(nodedata.GetPos(node));
                    UpdateSwitchUnknowns((ASwitch)current.Parent());
                }
            }
        }

        private void UpdateSwitchUnknowns(ASwitch aswitch)
        {
            ASwitchCase acase = null;

            while ((acase = aswitch.GetNextCase(acase)) != null)
            {
                foreach (AUnkLoopControl unk in acase.GetUnknowns())
                {
                    if (unk.GetDestination() > aswitch.End())
                    {
                        acase.ReplaceUnknown(unk, new AContinueStatement());
                    }
                    else
                    {
                        acase.ReplaceUnknown(unk, new ABreakStatement());
                    }
                }
            }
        }

        private ScriptRootNode GetLoop()
        {
            return GetEnclosingLoop(current);
        }

        private ScriptRootNode GetEnclosingLoop(ScriptDomNode start)
        {
            for (ScriptDomNode node = start; node != null; node = node.Parent())
            {
                if ((node is ADoLoop) || (node is AWhileLoop))
                {
                    return (ScriptRootNode)node;
                }
            }

            return null;
        }

        private ScriptRootNode GetBreakable()
        {
            for (ScriptDomNode node = current; node != null; node = node.Parent())
            {
                if ((node is ADoLoop) || (node is AWhileLoop)
                      || (node is ASwitchCase))
                {
                    return (ScriptRootNode)node;
                }
            }

            return null;
        }

        private AControlLoop GetLoop(AstNode destination, AstNode origin)
        {
            AstNode beforeJump = NodeUtils.GetPreviousCommand(origin, nodedata);
            if (NodeUtils.IsJzPastOne(beforeJump))
            {
                return new ADoLoop(nodedata.GetPos(destination), nodedata.GetPos(origin));
            }

            return new AWhileLoop(nodedata.GetPos(destination), nodedata.GetPos(origin));
        }

        private IAExpression RemoveIfAsExp()
        {
            AIf aif = (AIf)current;
            IAExpression exp = aif.Condition();
            current = (ScriptRootNode)current.Parent();
            current.RemoveChild(aif);
            aif.Parent(null);
            exp.Parent(null);
            return exp;
        }

        private IAExpression RemoveLastExp(bool forceOneOnly)
        {
            SubScriptLogger.Trace("removeLastExp: forceOneOnly=" + forceOneOnly + ", current=" +
                  current.GetType().Name + ", hasChildren=" + current.HasChildren());

            var trailingErrors = new List<ScriptDomNode>();
            while (current.HasChildren() && current.GetLastChild() is AErrorComment)
            {
                trailingErrors.Add(current.RemoveLastChild());
            }

            if (!current.HasChildren() && current is AIf)
            {
                for (int i = trailingErrors.Count - 1; i >= 0; i--)
                {
                    current.AddChild(trailingErrors[i]);
                }

                return RemoveIfAsExp();
            }

            ScriptDomNode anode = null;
            var foundExpressionStatements = new List<AExpressionStatement>();
            while (true)
            {
                if (!current.HasChildren())
                {
                    SubScriptLogger.Trace("removeLastExp: no more children, breaking");
                    break;
                }
                anode = current.RemoveLastChild();
                SubScriptLogger.Trace("removeLastExp: removed child=" + anode.GetType().Name);

                if (anode is IAExpression)
                {
                    SubScriptLogger.Trace("removeLastExp: found IAExpression, returning");
                    for (int i = foundExpressionStatements.Count - 1; i >= 0; i--)
                    {
                        current.AddChild(foundExpressionStatements[i]);
                    }
                    break;
                }
                if (anode is AVarDecl vardecl)
                {
                    if (vardecl.IsFcnReturn() && vardecl.Exp() != null)
                    {
                        IAExpression exp = vardecl.RemoveExp();
                        for (int i = foundExpressionStatements.Count - 1; i >= 0; i--)
                        {
                            current.AddChild(foundExpressionStatements[i]);
                        }
                        for (int i = trailingErrors.Count - 1; i >= 0; i--)
                        {
                            current.AddChild(trailingErrors[i]);
                        }
                        return exp;
                    }
                    if (!forceOneOnly && vardecl.Exp() != null)
                    {
                        IAExpression exp = vardecl.RemoveExp();
                        for (int i = foundExpressionStatements.Count - 1; i >= 0; i--)
                        {
                            current.AddChild(foundExpressionStatements[i]);
                        }
                        for (int i = trailingErrors.Count - 1; i >= 0; i--)
                        {
                            current.AddChild(trailingErrors[i]);
                        }
                        return exp;
                    }
                }
                else if (anode is AExpressionStatement)
                {
                    SubScriptLogger.Trace("removeLastExp: found AExpressionStatement, storing and continuing search");
                    foundExpressionStatements.Add((AExpressionStatement)anode);
                    anode = null;
                    continue;
                }
                SubScriptLogger.Trace("removeLastExp: skipping " + anode.GetType().Name + ", continuing search");
                anode = null;
            }

            if (anode == null && foundExpressionStatements.Count > 0)
            {
                SubScriptLogger.Trace("removeLastExp: no plain expression found, extracting from AExpressionStatement");
                int li = foundExpressionStatements.Count - 1;
                AExpressionStatement expstmt = foundExpressionStatements[li];
                foundExpressionStatements.RemoveAt(li);
                IAExpression exp = expstmt.Exp();
                if (exp != null)
                {
                    exp.Parent(null);
                    for (int i = foundExpressionStatements.Count - 1; i >= 0; i--)
                    {
                        current.AddChild(foundExpressionStatements[i]);
                    }
                    for (int i = trailingErrors.Count - 1; i >= 0; i--)
                    {
                        current.AddChild(trailingErrors[i]);
                    }
                    SubScriptLogger.Trace("removeLastExp: returning expression from AExpressionStatement");
                    return exp;
                }
            }

            if (anode == null)
            {
                return BuildPlaceholderParam(1);
            }

            if (!forceOneOnly
                  && anode is AVarRef
                  && !((AVarRef)anode).Var().IsAssigned()
                  && !((AVarRef)anode).Var().IsParam()
                  && current.HasChildren())
            {
                ScriptDomNode last = current.GetLastChild();
                if (last is IAExpression
                      && ReferenceEquals(((AVarRef)anode).Var(), ((IAExpression)last).Stackentry()))
                {
                    IAExpression exp = RemoveLastExp(false);
                    for (int i = trailingErrors.Count - 1; i >= 0; i--)
                    {
                        current.AddChild(trailingErrors[i]);
                    }

                    return exp;
                }

                if (last is AVarDecl && ReferenceEquals(((AVarRef)anode).Var(), ((AVarDecl)last).Var())
                      && ((AVarDecl)last).Exp() != null)
                {
                    IAExpression exp = RemoveLastExp(false);
                    for (int i = trailingErrors.Count - 1; i >= 0; i--)
                    {
                        current.AddChild(trailingErrors[i]);
                    }

                    return exp;
                }
            }

            for (int i = trailingErrors.Count - 1; i >= 0; i--)
            {
                current.AddChild(trailingErrors[i]);
            }

            return (IAExpression)anode;
        }

        private IAExpression GetLastExp()
        {
            ScriptDomNode anode = current.GetLastChild();
            if (!(anode is IAExpression))
            {
                if ((anode is AVarDecl) && ((AVarDecl)anode).IsFcnReturn())
                {
                    return ((AVarDecl)anode).Exp();
                }
                else
                {
                    SubScriptLogger.Trace(anode.ToString());
                    throw new InvalidOperationException("Last child not an expression " + anode);
                }
            }
            else
            {
                return (IAExpression)anode;
            }
        }

        private IAExpression GetPreviousExp(int pos)
        {
            ScriptDomNode node = current.GetPreviousChild(pos);
            if (node == null)
            {
                return null;
            }
            else if ((node is AVarDecl) && ((AVarDecl)node).IsFcnReturn())
            {
                return ((AVarDecl)node).Exp();
            }
            else
            {
                return !(node is IAExpression) ? null : (IAExpression)node;
            }
        }

        public void SetVarStructName(VarStruct varstruct)
        {
            if (varstruct.Name() == null)
            {
                int count = 1;
                var key = new DecompType(DecompType.VtStruct);
                object prev = varcounts[key];
                if (prev is int pc)
                {
                    count += pc;
                }

                varstruct.Name(varprefix, count);
                varcounts[key] = count;
            }
        }

        private void UpdateVarCount(Variable var)
        {
            int count = 1;
            DecompType key = var.Type();
            object prev = varcounts[key];
            if (prev is int pc)
            {
                count += pc;
            }

            var.Name(varprefix, count);
            varcounts[key] = count;
        }

        private void UpdateStructVar(ADestructCommand node)
        {
            AVarRef varref = (AVarRef)GetLastExp();
            int removesize = NodeUtils.StackSizeToPos(node.GetSizeRem());
            int savestart = NodeUtils.StackSizeToPos(node.GetOffset());
            int savesize = NodeUtils.StackSizeToPos(node.GetSizeSave());
            if (savesize > 1)
            {
                throw new InvalidOperationException("Ah-ha!  A nested struct!  Now I have to code for that.  *sob*");
            }
            else
            {
                SetVarStructName((VarStruct)varref.Var());
                Variable var = (Variable)stack.Get(removesize - savestart);
                varref.ChooseStructElement(var);
            }
        }

        private AVarRef GetVarToAssignTo(AStackCommand node)
        {
            int loc = NodeUtils.StackOffsetToPos(node.GetOffset());
            if (NodeUtils.IsGlobalStackOp(node))
            {
                loc--;
            }

            Variable var;
            if (NodeUtils.IsGlobalStackOp(node))
            {
                var = (Variable)subdata.GetGlobalStack().Get(loc);
            }
            else
            {
                if (!(stack.Get(loc) is Variable))
                {
                    SubScriptLogger.Trace("not a variable at loc " + loc);
                    SubScriptLogger.Trace(stack.ToString());
                }

                var = (Variable)stack.Get(loc);
            }

            var.Assigned();
            return new AVarRef(var);
        }

        private bool IsReturn(ACopyDownSpCommand node)
        {
            return !root.ReturnType().Equals((byte)0) && stack.Size() == NodeUtils.StackOffsetToPos(node.GetOffset());
        }

        private bool IsReturn(AJumpCommand node)
        {
            AstNode dest = nodedata.GetDestination(node);
            AstNode destChild = dest != null ? NodeUtils.GetCommandChild(dest) : null;
            int nodePos = nodedata.GetPos(node);
            int destPos = dest != null ? nodedata.GetPos(dest) : -1;

            SubScriptLogger.Trace("isReturn: pos=" + nodePos + ", destPos=" + destPos +
                  ", destType=" + (dest != null ? dest.GetType().Name : "null") +
                  ", destChildType=" + (destChild != null ? destChild.GetType().Name : "null"));

            if (NodeUtils.IsReturn(destChild))
            {
                SubScriptLogger.Trace("isReturn: returning true (destChild is Return)");
                return true;
            }
            else if ((dest is AMoveSpCommand))
            {
                AstNode afterdest = NodeUtils.GetNextCommand(dest, nodedata);
                bool result = afterdest == null;
                SubScriptLogger.Trace("isReturn: dest is MoveSpCommand, afterdest=" +
                      (afterdest != null ? nodedata.GetPos(afterdest) + " (" + afterdest.GetType().Name + ")" : "null") +
                      ", returning " + result);
                return result;
            }
            else
            {
                SubScriptLogger.Trace("isReturn: returning false");
                return false;
            }
        }

        private AVarRef GetVarToAssignTo(ACopyDownSpCommand node)
        {
            return (AVarRef)GetVar(NodeUtils.StackSizeToPos(node.GetSize()),
                  NodeUtils.StackOffsetToPos(node.GetOffset()), stack, true, this);
        }

        private AVarRef GetVarToAssignTo(ACopyDownBpCommand node)
        {
            return (AVarRef)GetVar(
                  NodeUtils.StackSizeToPos(node.GetSize()),
                  NodeUtils.StackOffsetToPos(node.GetOffset()),
                  subdata.GetGlobalStack(),
                  true,
                  subdata.GlobalState());
        }

        private IAExpression GetVarToCopy(ACopyTopSpCommand node)
        {
            return GetVar(NodeUtils.StackSizeToPos(node.GetSize()), NodeUtils.StackOffsetToPos(node.GetOffset()),
                  stack, false, this);
        }

        private IAExpression GetVarToCopy(ACopyTopBpCommand node)
        {
            return GetVar(
                  NodeUtils.StackSizeToPos(node.GetSize()),
                  NodeUtils.StackOffsetToPos(node.GetOffset()),
                  subdata.GetGlobalStack(),
                  false,
                  subdata.GlobalState());
        }

        private IAExpression GetVar(int copy, int loc, LocalVarStack stack, bool assign, SubScriptState state)
        {
            bool isstruct = copy > 1;
            StackEntry entry = stack.Get(loc);
            if (!(entry is Variable) && assign)
            {
                throw new InvalidOperationException("Attempting to assign to a non-variable");
            }
            else if ((entry is Const))
            {
                return new AConst((Const)entry);
            }
            else
            {
                Variable var = (Variable)entry;
                if (!isstruct)
                {
                    if (assign)
                    {
                        var.Assigned();
                    }

                    return new AVarRef(var);
                }
                else if (var.IsStruct())
                {
                    if (assign)
                    {
                        var.Varstruct().Assigned();
                    }

                    state.SetVarStructName(var.Varstruct());
                    return new AVarRef(var.Varstruct());
                }
                else
                {
                    VarStruct newstruct = new VarStruct();
                    newstruct.AddVar(var);

                    for (int i = loc - 1; i > loc - copy; i--)
                    {
                        if (i < 1 || i > stack.Size())
                        {
                            break;
                        }
                        var = (Variable)stack.Get(i);
                        newstruct.AddVar(var);
                    }

                    if (assign)
                    {
                        newstruct.Assigned();
                    }

                    subdata.AddStruct(newstruct);
                    state.SetVarStructName(newstruct);
                    return new AVarRef(newstruct);
                }
            }
        }

        private List<AVarRef> GetParams(int paramcount)
        {
            var list = new List<AVarRef>();
            for (int i = 1; i <= paramcount; i++)
            {
                Variable var = (Variable)stack.Get(i);
                var.Name("Param", i);
                list.Add(new AVarRef(var));
            }

            return list;
        }

        private List<IAExpression> RemoveFcnParams(AJumpToSubroutine node)
        {
            var fparams = new List<IAExpression>();
            int paramcount = subdata.GetState(nodedata.GetDestination(node)).GetParamCount();
            int i = 0;

            while (i < paramcount)
            {
                IAExpression exp;
                try
                {
                    exp = RemoveLastExp(false);
                }
                catch (Exception)
                {
                    exp = BuildPlaceholderParam(i + 1);
                }

                int expSize = GetExpSize(exp);
                i += expSize <= 0 ? 1 : expSize;
                fparams.Add(exp);
            }

            return fparams;
        }

        private int GetExpSize(IAExpression exp)
        {
            if (exp is AVarRef)
            {
                return ((AVarRef)exp).Var().Size();
            }

            return 1;
        }

        private AVarRef BuildPlaceholderParam(int ordinal)
        {
            Variable placeholder = new Variable(new DecompType(DecompType.VtInvalid));
            placeholder.Name("__unknown_param_" + ordinal);
            placeholder.IsParam(true);
            return new AVarRef(placeholder);
        }

        private List<IAExpression> RemoveActionParams(AActionCommand node)
        {
            var fparams = new List<IAExpression>();
            int nodePos = nodedata.GetPos(node);
            SubScriptLogger.Trace("removeActionParams: pos=" + nodePos + ", current=" + current.GetType().Name +
                  ", hasChildren=" + current.HasChildren() + ", childrenCount=" + (current.HasChildren() ? current.Size() : 0));

            List<DecompType> paramtypes;
            try
            {
                paramtypes = NodeUtils.GetActionParamTypes(node, actions);
                SubScriptLogger.Trace("removeActionParams: got paramtypes, count=" + (paramtypes != null ? paramtypes.Count : 0));
            }
            catch (Exception)
            {
                int paramcount = NodeUtils.GetActionParamCount(node);
                SubScriptLogger.Trace("removeActionParams: action metadata missing, using paramcount=" + paramcount);
                for (int i = 0; i < paramcount; i++)
                {
                    try
                    {
                        IAExpression exp = RemoveLastExp(false);
                        SubScriptLogger.Trace("removeActionParams: removed param " + (i + 1) + "=" + exp.GetType().Name);
                        fparams.Add(exp);
                    }
                    catch (Exception)
                    {
                        SubScriptLogger.Trace("removeActionParams: failed to remove param " + (i + 1) + ", using placeholder");
                        fparams.Add(BuildPlaceholderParam(i + 1));
                    }
                }

                SubScriptLogger.Trace("removeActionParams: returning " + fparams.Count + " params (metadata missing case)");
                return fparams;
            }

            int argBytes = NodeUtils.GetActionParamCount(node);
            int paramcount2 = paramtypes.Count;

            SubScriptLogger.Trace("removeActionParams: argBytes=" + argBytes + ", paramtypes.Count=" + paramtypes.Count +
                  ", using paramcount=" + paramcount2);

            for (int i = 0; i < paramcount2; i++)
            {
                DecompType paramtype = paramtypes[i];
                IAExpression exp;
                try
                {
                    SubScriptLogger.Trace("removeActionParams: removing param " + (i + 1) + "/" + paramcount2 + ", type=" + paramtype.TypeSize() +
                          ", current hasChildren=" + current.HasChildren());
                    if (paramtype.Equals(DecompType.VtVector))
                    {
                        exp = GetLastExp();
                        if (!exp.Stackentry().Type().Equals(DecompType.VtVector)
                              && !exp.Stackentry().Type().Equals(DecompType.VtStruct))
                        {
                            IAExpression exp3 = RemoveLastExp(false);
                            IAExpression exp2 = RemoveLastExp(false);
                            IAExpression exp1 = RemoveLastExp(false);
                            exp = new AVectorConstExp(exp1, exp2, exp3);
                        }
                        else
                        {
                            exp = RemoveLastExp(false);
                        }
                    }
                    else
                    {
                        exp = RemoveLastExp(false);
                    }

                    SubScriptLogger.Trace("removeActionParams: successfully removed param " + (i + 1) + "=" + exp.GetType().Name);
                }
                catch (Exception expEx)
                {
                    SubScriptLogger.Trace("removeActionParams: failed to remove param " + (i + 1) + ", using placeholder: " + expEx.Message);
                    exp = BuildPlaceholderParam(i + 1);
                }

                fparams.Add(exp);
            }

            SubScriptLogger.Trace("removeActionParams: returning " + fparams.Count + " params, remaining children=" + (current.HasChildren() ? current.Size() : 0));
            return fparams;
        }

        private byte GetFcnId(AJumpToSubroutine node)
        {
            return subdata.GetState(nodedata.GetDestination(node)).GetId();
        }

        private DecompType GetFcnType(AJumpToSubroutine node)
        {
            return subdata.GetState(nodedata.GetDestination(node)).Type();
        }

        private int GetNextCommand(AJumpCommand node)
        {
            return nodedata.GetPos(node) + 6;
        }

        private int GetPriorToDestCommand(AJumpCommand node)
        {
            return nodedata.GetPos(nodedata.GetDestination(node)) - 2;
        }
    }
}
