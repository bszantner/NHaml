﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NHaml4.Compilers
{
    public interface ITemplateClassBuilder
    {
        void Append(string content);
        void AppendFormat(string content, params object[] args);
        void AppendNewLine();
        void AppendCodeToString(string codeSnippet);
        void AppendCodeSnippet(string codeSnippet, bool containsChildren);
        void RenderEndBlock();
        void AppendVariable(string variableName);
        string Build(string className, Type baseType, IEnumerable<string> imports);
        void AppendAttributeNameValuePair(string name, IEnumerable<string> valueFragments, char quoteToUse);
        void AppendSelfClosingTagSuffix();
        void AppendDocType(string docTypeId);
    }
}
