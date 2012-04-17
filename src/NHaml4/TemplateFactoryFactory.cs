using System;
using System.Collections.Generic;
using NHaml4.TemplateResolution;
using NHaml4.Parser;
using NHaml4.Compilers;
using NHaml4.Walkers;
using NHaml4.IO;
using NHaml4.Walkers.CodeDom;
using NHaml4.Compilers.Abstract;
using System.Linq;
using NHaml4.Parser.Rules;
using System.IO;

namespace NHaml4
{
    public class TemplateFactoryFactory : ITemplateFactoryFactory
    {
        private readonly ITreeParser _treeParser;
        private readonly IDocumentWalker _treeWalker;
        private readonly ITemplateFactoryCompiler _templateFactoryCompiler;
        private IEnumerable<string> _imports;
        private IEnumerable<string> _referencedAssemblyLocations;
        private IDictionary<string, HamlDocument> _hamlDocumentCache = new Dictionary<string, HamlDocument>();

        public TemplateFactoryFactory(Walkers.CodeDom.HamlHtmlOptions hamlOptions)
            : this(hamlOptions, new List<string>(), new List<string>())
        { }

        public TemplateFactoryFactory(Walkers.CodeDom.HamlHtmlOptions hamlOptions, IList<string> imports, IList<string> referencedAssemblyLocations)
            : this(new HamlTreeParser(new HamlFileLexer()),
                    new HamlDocumentWalker(new CodeDomClassBuilder(), hamlOptions),
                    new CodeDomTemplateCompiler(new CSharp2TemplateTypeBuilder()),
            imports, referencedAssemblyLocations)
        { }

        public TemplateFactoryFactory(ITreeParser treeParser, IDocumentWalker treeWalker,
            ITemplateFactoryCompiler templateCompiler, IEnumerable<string> imports, IEnumerable<string> referencedAssemblyLocations)
        {
            _treeParser = treeParser;
            _treeWalker = treeWalker;
            _templateFactoryCompiler = templateCompiler;
            _imports = imports;
            _referencedAssemblyLocations = referencedAssemblyLocations;
        }

        public TemplateFactory CompileTemplateFactory(string className, IViewSource viewSource)
        {
            return CompileTemplateFactory(className, new ViewSourceCollection { viewSource }, typeof(TemplateBase.Template));
        }

        public TemplateFactory CompileTemplateFactory(string className, IViewSource viewSource, Type baseType)
        {
            return CompileTemplateFactory(className, new ViewSourceCollection { viewSource }, baseType);
        }

        public TemplateFactory CompileTemplateFactory(string className, ViewSourceCollection viewSourceList)
        {
            return CompileTemplateFactory(className, viewSourceList, typeof(TemplateBase.Template));
        }

        public TemplateFactory CompileTemplateFactory(string className, ViewSourceCollection viewSourceList, Type baseType)
        {
            var hamlDocument = BuildHamlDocument(viewSourceList);
            string templateCode = _treeWalker.Walk(hamlDocument, className, baseType, _imports);
            var templateFactory = _templateFactoryCompiler.Compile(templateCode, className, _referencedAssemblyLocations);
            return templateFactory;
        }

        public HamlDocument BuildHamlDocument(ViewSourceCollection viewSourceList)
        {
            var hamlDocument = HamlDocumentCacheGetOrAdd(viewSourceList.First().FileName,
                () => _treeParser.ParseViewSource(viewSourceList.First()));

            HamlNodePartial partial;
            while ((partial = hamlDocument.GetNextUnresolvedPartial()) != null)
            {
                try
                {
                    var viewSource = viewSourceList.GetByPartialName(partial.Content);
                    var partialDocument = HamlDocumentCacheGetOrAdd(viewSource.FileName,
                        () => _treeParser.ParseViewSource(viewSource));
                    partial.SetDocument(partialDocument);
                }
                catch (InvalidOperationException)
                {
                    throw new PartialNotFoundException(partial.Content);
                }
            }
            return hamlDocument;
        }

        private HamlDocument HamlDocumentCacheGetOrAdd(string key, Func<HamlDocument> getter)
        {
            HamlDocument result;
            bool templateInCache = _hamlDocumentCache.TryGetValue(key, out result);
            if (templateInCache == false)
            {
                result = getter();
                _hamlDocumentCache[key] = result;
            }

            return result;
        }
    }
}