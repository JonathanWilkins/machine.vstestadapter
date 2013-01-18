﻿using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Machine.VSTestAdapter
{
    public class SpecificationDiscoverer : ISpecificationDiscoverer
    {
        private const int PdbHiddenLine = 0xFEEFEE;

        public string AssemblyFilename { get; set; }

        public SpecificationDiscoverer()
        {
        }

        public IEnumerable<MSpecTestCase> EnumerateSpecs(string assemblyFilePath)
        {
            assemblyFilePath = Path.GetFullPath(assemblyFilePath);
            if (!File.Exists(assemblyFilePath))
            {
                throw new ArgumentException("Could not find file: " + assemblyFilePath);
            }

            this.AssemblyFilename = assemblyFilePath;

            List<MSpecTestCase> list = new List<MSpecTestCase>();

            // statically inspect the types in the assembly using mono.cecil
            foreach (TypeDefinition type in AssemblyDefinition.ReadAssembly(this.AssemblyFilename, new ReaderParameters() { ReadSymbols = true }).MainModule.GetTypes())
            {
                // if a type is an It delegate generate some test case info for it
                foreach (FieldDefinition fieldDefinition in type.Fields.Where(x => x.FieldType.FullName == "Machine.Specifications.It" && !x.Name.Contains("__Cached")))
                {
                    string typeName = NormalizeCecilTypeName(type.Name);
                    string typeFullName = NormalizeCecilTypeName(type.FullName);

                    MSpecTestCase testCase = new MSpecTestCase()
                    {
                        ContextType = typeName,
                        ContextFullType = typeFullName,
                        SpecificationName = fieldDefinition.Name
                    };

                    // get the source code location for the It delegate from the PDB file using mono.cecil.pdb
                    this.UpdateTestCaseWithLocation(type, testCase);
                    list.Add(testCase);
                }
            }
            return list.Select(x => x);
        }

        private string NormalizeCecilTypeName(string cecilTypeName)
        {
            return cecilTypeName.Replace('/', '+');
        }

        public bool SourceDirectoryContainsMSpec(string assemblyFileName)
        {
            return File.Exists(Path.Combine(Path.GetDirectoryName(assemblyFileName), "Machine.Specifications.dll"));
        }

        public bool AssemblyContainsMSpecReference(string assemblyFileName)
        {
            AssemblyDefinition asmDef = AssemblyDefinition.ReadAssembly(assemblyFileName);
            foreach (AssemblyNameReference anrRef in asmDef.MainModule.AssemblyReferences)
            {
                if (anrRef.FullName.StartsWith("Machine.Specifications"))
                {
                    return true;
                }
            }

            return false;
        }

        private void UpdateTestCaseWithLocation(TypeDefinition type, MSpecTestCase testCase)
        {
            if (!type.HasMethods)
            {
                return;
            }

            string fieldFullName = testCase.SpecificationName.Replace(" ", "_");
            MethodDefinition methodDefinition = type.Methods.Where(x => x.IsConstructor && x.Parameters.Count == 0).SingleOrDefault();
            if (methodDefinition.HasBody)
            {
                // check if there is a subject attribute
                if (type.HasCustomAttributes)
                {
                    List<CustomAttribute> list = type.CustomAttributes.Where(x => x.AttributeType.FullName == "Machine.Specifications.SubjectAttribute").ToList();
                    if (list.Count > 0 && list[0].ConstructorArguments.Count > 0)
                    {
                        testCase.SubjectName = Enumerable.First<CustomAttributeArgument>((IEnumerable<CustomAttributeArgument>)list[0].ConstructorArguments).Value.ToString();
                    }
                }

                // now find the source code location
                Instruction instruction = methodDefinition.Body.Instructions.Where(x => x.Operand != null &&
                                                              x.Operand.GetType().IsAssignableFrom(typeof(FieldDefinition)) &&
                                                              ((MemberReference)x.Operand).Name == fieldFullName).SingleOrDefault();

                while (instruction != null)
                {
                    if (instruction.SequencePoint != null && instruction.SequencePoint.StartLine != PdbHiddenLine)
                    {
                        testCase.CodeFilePath = instruction.SequencePoint.Document.Url;
                        testCase.LineNumber = instruction.SequencePoint.StartLine;
                        break;
                    }
                    instruction = instruction.Previous;
                }
            }
        }
    }
}