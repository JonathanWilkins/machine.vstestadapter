﻿using System;
using System.Globalization;

namespace Machine.VSTestAdapter.Helpers
{
#if !NETSTANDARD
    [Serializable]
#endif
    public class VisualStudioTestIdentifier
    {
        public VisualStudioTestIdentifier()
        {
        }


        public VisualStudioTestIdentifier(string containerTypeFullName, string fieldName)
            : this(String.Format(CultureInfo.InvariantCulture, "{0}::{1}", containerTypeFullName, fieldName))
        {
        }

        public VisualStudioTestIdentifier(string fullyQualifiedName)
        {
            FullyQualifiedName = fullyQualifiedName;
        }

        public string FullyQualifiedName { get; private set; }

        public string FieldName {
            get {
                return FullyQualifiedName.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[1];
            }
        }

        public string ContainerTypeFullName {
            get {
                return FullyQualifiedName.Split(new string[] { "::" }, StringSplitOptions.RemoveEmptyEntries)[0];
            }
        }

        public override bool Equals(object obj)
        {
            VisualStudioTestIdentifier test = obj as VisualStudioTestIdentifier;
            if (test != null)
                return FullyQualifiedName.Equals(test.FullyQualifiedName, StringComparison.Ordinal);
            else
                return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return FullyQualifiedName.GetHashCode();
        }

    }
}