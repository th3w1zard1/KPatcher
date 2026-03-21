// Copyright (c) 2021-2025 DeNCS contributors
// Licensed under the MIT License.
// See NOTICE and licenses/DeNCS-MIT.txt in the project root.

using System;
using System.Collections.Generic;
using NCSDecomp.Core.Utils;

namespace NCSDecomp.Core.Stack
{
    /// <summary>
    /// Variable aggregating multiple fields into a struct-like stack entry (DeNCS VarStruct.java).
    /// </summary>
    public class VarStruct : Variable
    {
        protected List<Variable> vars = new List<Variable>();
        protected StructType structtype;

        public VarStruct()
            : base(new DecompType(DecompType.VtStruct))
        {
            size = 0;
            structtype = new StructType();
        }

        public VarStruct(StructType st)
            : this()
        {
            structtype = st;
            List<DecompType> types = st.Types();
            foreach (DecompType t in types)
            {
                StructType nested = t as StructType;
                if (nested != null)
                {
                    AddVar(new VarStruct(nested));
                }
                else
                {
                    AddVar(new Variable(t));
                }
            }
        }

        public override void Close()
        {
            base.Close();
            if (vars != null)
            {
                for (int i = 0; i < vars.Count; i++)
                {
                    vars[i].Close();
                }

                vars = null;
            }

            if (structtype != null)
            {
                structtype.Close();
            }

            structtype = null;
        }

        public void AddVar(Variable var)
        {
            vars.Insert(0, var);
            var.Varstruct(this);
            structtype.AddType(var.Type());
            size = size + var.Size();
        }

        public void AddVarStackOrder(Variable var)
        {
            vars.Add(var);
            var.Varstruct(this);
            structtype.AddTypeStackOrder(var.Type());
            size = size + var.Size();
        }

        public override void Name(string prefix, byte count)
        {
            name = prefix + "struct" + count;
        }

        public string Name()
        {
            return name;
        }

        public void StructType(StructType st)
        {
            structtype = st;
        }

        public override string ToString()
        {
            return name;
        }

        public string TypeName()
        {
            return structtype.TypeName();
        }

        public override string ToDeclString()
        {
            return structtype.ToDeclString() + " " + name;
        }

        public void UpdateNames()
        {
            if (structtype.IsVector())
            {
                vars[0].Name("z");
                vars[1].Name("y");
                vars[2].Name("x");
            }
            else
            {
                for (int i = 0; i < vars.Count; i++)
                {
                    vars[i].Name(structtype.ElementName(vars.Count - i - 1));
                }
            }
        }

        public override void Assigned()
        {
            for (int i = 0; i < vars.Count; i++)
            {
                vars[i].Assigned();
            }
        }

        public override void AddedToStack(LocalVarStack stack)
        {
            for (int i = 0; i < vars.Count; i++)
            {
                vars[i].AddedToStack(stack);
            }
        }

        public bool Contains(Variable var)
        {
            return vars.Contains(var);
        }

        public StructType StructType()
        {
            return structtype;
        }

        public override StackEntry GetElement(int stackpos)
        {
            int pos = 0;
            for (int i = vars.Count - 1; i >= 0; i--)
            {
                StackEntry entry = vars[i];
                pos += entry.Size();
                if (pos == stackpos)
                {
                    return entry.GetElement(1);
                }

                if (pos > stackpos)
                {
                    return entry.GetElement(pos - stackpos + 1);
                }
            }

            throw new InvalidOperationException("Stackpos was greater than stack size");
        }

        public VarStruct Structify(int firstelement, int count, SubroutineAnalysisData subdata)
        {
            int pos = 0;
            for (int vi = 0; vi < vars.Count; vi++)
            {
                StackEntry entry = vars[vi];
                pos += entry.Size();
                if (pos == firstelement)
                {
                    var varstruct = new VarStruct();
                    varstruct.AddVarStackOrder((Variable)entry);
                    int j = vi + 1;
                    entry = j < vars.Count ? vars[j] : null;
                    for (int bound = pos + (entry != null ? entry.Size() : 0);
                         entry != null && bound <= firstelement + count - 1;
                         bound += entry.Size())
                    {
                        vars.RemoveAt(j);
                        varstruct.AddVarStackOrder((Variable)entry);
                        if (j >= vars.Count)
                        {
                            break;
                        }

                        entry = vars[j];
                    }

                    subdata.AddStruct(varstruct);
                    vars[vi] = varstruct;
                    return varstruct;
                }

                if (pos == firstelement + count - 1)
                {
                    return (VarStruct)entry;
                }

                if (pos > firstelement + count - 1)
                {
                    return ((VarStruct)entry).Structify(firstelement - (pos - entry.Size()), count, subdata);
                }
            }

            return null;
        }
    }
}
