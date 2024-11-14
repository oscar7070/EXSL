using System.Text;
using System.Text.RegularExpressions;

namespace ExtremeEngine.HLSLTools
{
    /// <summary> EXSLANG used to handle HLSL with ExtremeEngine extensions(EXSL).</summary>
    public static partial class EXSLANG
    {

        public const string DefineKeyword = "#define",
            PragmaKeyword = "#pragma",
            IncludeKeyword = "#include",
            AttributeStartKeyC = "[", AttributeEndKeyC = "]",
            AttributeStartKeyCPP11 = "[[", AttributeEndKeyCPP11 = "]]",
            ConstKeyword = "const", StaticKeyword = "static",
            VoidKeyword = "void",
            ReturnKeyword = "return",
            nullKeyword = "null";

        public enum ContextType
        {
            Null,
            Pragma,
            DefineStart,
            DefineEnd,
            Include,
            Attribute,
            MethodStart,
            MethodEnd,

            IL,
            ILDefine,

            ILAttribute,
            ILNodeStart,
            ILNodeEnd,
            ILCommentNodeStart,
            ILCommentNodeEnd,
        }

        public enum ShaderStage { Main, PostProcessing };

        /// <summary> HLSL writer with EXSL ExtremeEngine extension support.</summary>
        public partial class HLSLWriter
        {

            public const int DefaultSpacesCountOnStage = 4;
            public int SpacesCountOnStage { get; private set; } = 4;
            public int SpacingStage { get; private set; } = 0;

            public readonly HLSL.SyntaxStyle SyntaxStyle = HLSL.SyntaxStyle.CPP11;
            private readonly StringBuilder Data = new();

            public HLSLWriter(bool isIL) : this(isIL, HLSL.SyntaxStyle.CPP11, DefaultSpacesCountOnStage) { }
            public HLSLWriter(bool isIL, HLSL.SyntaxStyle syntax = HLSL.SyntaxStyle.CPP11, int spacesCountOnStage = DefaultSpacesCountOnStage)
            {
                IsIL = isIL; SyntaxStyle = syntax; SpacesCountOnStage = spacesCountOnStage;

                if (isIL)
                {
                    IL_DefineEXISLShader();
                    IL_DefineEXISLVersion();
                }
            }

            public string GetWrittenData() => Data.ToString();

            public void Write(char chr) => Data.Append(chr);
            public void Write(string str) => Data.Append(str);

            public void Comment(string comment, bool newLineOnEnd = true)
            {
                Write("// " + comment);
                if (newLineOnEnd)
                    NewLine();
            }

            public void StartComments(string comments) => Write("/* " + comments);
            public void EndComments() => Write(" */");

            public void EXSL_Type(ShaderType type, bool newLineOnEnd = true) => PragmaDirective("EXSL_Type " + GetEXSLShaderTypeDefine(type), newLineOnEnd);
            public void EXSL_Stage(ShaderStage stage, bool newLineOnEnd = true) => PragmaDirective("EXSL_Stage " + GetEXSLShaderStageDefine(stage), newLineOnEnd);
            public void EXSL_ShaderModule(int major, int minor, bool newLineOnEnd = true) => PragmaDirective("EXSL_ShaderModule " + major + "." + minor, newLineOnEnd);

            public void DefineMacro(string macro, bool newLineOnEnd = true)
            {
                Write(DefineKeyword + ' ' + macro);
                if (newLineOnEnd)
                    NewLine();
            }

            public void PragmaDirective(string directive, bool newLineOnEnd = true)
            {
                Write(PragmaKeyword + ' ' + directive);
                if (newLineOnEnd)
                    NewLine();
            }

            public void Include(string include, bool newLineOnEnd = true)
            {
                Write(IncludeKeyword + " \"" + include + "\"");
                if (newLineOnEnd)
                    NewLine();
            }

            public void DefineAttribute(string attribute, bool newLineOnEnd = true, params HLSLParameter[] parameters)
            {
                switch (SyntaxStyle)
                {
                    default: //C++11
                        Write(AttributeStartKeyCPP11 + attribute);
                        if (parameters != null)
                            WriteParameters(true, parameters);
                        Write(AttributeEndKeyCPP11);
                        break;
                    case (HLSL.SyntaxStyle.C):
                        Write(AttributeStartKeyC + attribute);
                        if (parameters != null)
                            WriteParameters(true, parameters);
                        Write(AttributeEndKeyC);
                        break;
                }
                if (newLineOnEnd)
                    NewLine();
            }

            public void WriteParameters(bool addBrackets = true, params HLSLParameter[] parameters)
            {
                if (addBrackets)
                    Write("(");

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                        Write(", ");
                    Write(parameters[i].Name);
                }

                if (addBrackets)
                    Write(")");
            }

            public void WriteSemantic(string semantic) => Write(" : " + semantic);

            public void DefineField(HLSLField field, bool newLineOnEnd = true)
            {
                for (int i = 0; i < field.Attributes.Length; i++) DefineAttribute(field.Attributes[i].Name, false);
                if (field.Static) Write("static ");
                if (field.Const) Write("const ");

                DefineParameter(field.Parameter, false);

                if (field.Semantic != null) WriteSemantic(field.Semantic);
                if (newLineOnEnd)
                    EndLine();
            }

            public void DefineParameter(HLSLParameter parameter, bool endLine = true)
            {
                Write(parameter.Type.Name + " " + parameter.Name);
                if (endLine)
                    EndLine();
            }

            public void StartMethod(bool _static = false, string name = "main", params HLSLParameter[] parameters) => StartMethod(_static, null, name, parameters);
            public void StartMethod<T>(bool _static = false, string name = "main", params HLSLParameter[] parameters) where T : HLSLType => StartMethod(_static, Activator.CreateInstance<T>(), name, parameters);
            public void StartMethod(bool _static, HLSLType? returnHLSLType, string name = "main", params HLSLParameter[] parameters)
            {
                if (_static)
                    Write(StaticKeyword + ' ');

                if (returnHLSLType != null)
                    Write(returnHLSLType.Name);
                else
                    Write(VoidKeyword);

                Write(" " + name + "(");
                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                        Write(", ");
                    Write(parameters[i].Type.Name + " " + parameters[i].Name);
                }
                Write(")");
                NewLine();
                StartBracket();
                NewLine();
            }

            public void EndMethod(HLSLParameter? parameterToReturn = null, bool newLineOnEnd = true)
            {
                if (parameterToReturn != null)
                    Return(parameterToReturn);
                EndBracket();
                if (newLineOnEnd)
                    NewLine();
            }

            public void Return(HLSLParameter? parameter = null)
            {
                Write(ReturnKeyword);
                if (parameter != null)
                    Write(" " + parameter.Value.Name);
                EndLine(true);
            }

            public void EndLine(bool ignoreSpacingStages = false)
            {
                EndLineWithoutNewLine();
                NewLine(1, ignoreSpacingStages);
            }

            public void EndLineWithoutNewLine() => Write(";");

            public void NewLine(int count = 1, bool ignoreSpacingStages = false)
            {
                for (int i = 0; i < count; i++)
                    Write("\n");

                if (!ignoreSpacingStages)
                    for (int i = 0; i < SpacingStage * SpacesCountOnStage; i++)
                        Write(' ');
            }

            public void Brackets() => Write("{}");
            public void StartBracket(bool dontChangeSpacingStagesCount = false)
            {
                Write("{");
                if (!dontChangeSpacingStagesCount)
                    SpacingStage++;
            }

            public void EndBracket(bool dontChangeSpacingStagesCount = false)
            {
                if (!dontChangeSpacingStagesCount)
                    SpacingStage--;
                Write("}");
            }
        }

        /// <summary> HLSL reader with EXSL ExtremeEngine extension support.</summary>
        public partial class HLSLReader
        {

            private readonly StringReader Reader;
            public StringReader GetReader() => Reader;
            public string? CurrentLine { get; private set; } = null;
            public ContextType CurrentContext { get; private set; } = ContextType.Null;
            public readonly HLSL.SyntaxStyle SyntaxStyle = HLSL.SyntaxStyle.CPP11;

            public HLSLReader(string data, HLSL.SyntaxStyle syntax = HLSL.SyntaxStyle.CPP11)
            {
                Reader = new(data);
                SyntaxStyle = syntax;
            }

            public string? Read(bool trim = true, bool removeComments = true, bool updateCurrentContext = true)
            {
                CurrentLine = Reader.ReadLine();
                if (CurrentLine == null)
                    return CurrentLine;

                string comments = @"(//.*?$)|(/\*.*?\*/)";

                if (removeComments)
                    CurrentLine = Regex.Replace(CurrentLine, comments, string.Empty);

                if (trim)
                    CurrentLine.Trim();

                if (updateCurrentContext)
                    CurrentContext = GetContextType(CurrentLine);

                return CurrentLine;
            }

            public ContextType GetContextType(string line)
            {
                string[] words = line.Trim().Split(' ');

                string attrStart = HLSL.GetAttributeStart(HLSL.SyntaxStyle.C);
                string attrEnd = HLSL.GetAttributeStart(SyntaxStyle);

                if (words[0] == attrStart)
                    return ContextType.Attribute;
                else if (words[0] == ILAttributeStart + attrStart)
                    return ContextType.ILAttribute;

                switch (words[0])
                {
                    case (PragmaKeyword): return ContextType.Pragma;
                    case (DefineKeyword):
                        return ContextType.DefineStart;
                    case (IncludeKeyword): return ContextType.Include;

                    case (ILKeyword): return ContextType.IL;
                    case (ILDefineKeyword): return ContextType.ILDefine;

                    case (ILPropertyStart + ILNodeKey):
                        return ContextType.ILNodeStart;
                    case (ILPropertyStart + ILCommentNodeKey):
                        return ContextType.ILCommentNodeStart;
                }
                return ContextType.Null;
            }

            /// <summary> Get's words after #pragma.</summary>
            public string[]? GetPragma()
            {
                string pattern = PragmaKeyword + @"\s+(.*)";

                var match = Regex.Match(CurrentLine, pattern);
                if (match.Success)
                    return match.Groups[1].Value.Trim().Split(' ');

                return null;
            }

            /// <summary> Get's path after #include.</summary>
            public string[]? GetInclude()
            {
                string pattern = IncludeKeyword + '"' + @"\s+(.*)" + '"';

                var match = Regex.Match(CurrentLine, pattern);
                if (match.Success)
                    return match.Groups[1].Value.Trim().Split(' ');

                return null;
            }

            public string? GetMethod(out HLSLType? outType, out string? methodName, out IEnumerable<HLSLParameter>? inParameters)
            {
                outType = null; methodName = null; inParameters = null;

                string pattern = @"@(?<ilNode>[\w\.]+)\s+(?<returnType>\w+)\s+(?<methodName>\w+)\((?<parameters>[^\)]+)\)";
                var match = Regex.Match(CurrentLine, pattern);

                if (match.Success)
                {
                    string matchAttribute = match.Groups["ilNode"].Value;
                    string matchOutType = match.Groups["returnType"].Value;
                    methodName = match.Groups["methodName"].Value;
                    string matchParameters = match.Groups["parameters"].Value;

                    Debug.Log(matchAttribute); Debug.Log(matchOutType); Debug.Log(methodName); Debug.Log(matchParameters);

                    if (matchOutType != VoidKeyword)
                        outType = HLSL.CreateTypeFromName(matchOutType);

                    Debug.Log(outType);
                }
                return null;
            }
        }

        public static string GetEXSLShaderTypeDefine(ShaderType type)
        {
            switch (type)
            {
                default: /*case Shader.ShaderType.FragmentOrPixel:*/ return "fragment";
                case ShaderType.Vertex: return "vertex";
                case ShaderType.Geometry: return "geometry";
                case ShaderType.TessellationControlOrHull: return "tessellationControl";
                case ShaderType.TessellationEvaluationOrDomain: return "tessellationEvaluation";
                case ShaderType.Compute: return "compute";
            }
        }

        public static string GetEXSLShaderStageDefine(ShaderStage stage)
        {
            switch (stage)
            {
                default: /*case stage.ShaderStage.Main:*/ return "main";
                case ShaderStage.PostProcessing: return "postProcessing";
            }
        }

        public static string GetEXSLShaderFragmentInput(ShaderStage stage)
        {
            switch (stage)
            {
                default: /*case stage.ShaderStage.Main:*/ return "EXSLFragVSInput";
                case ShaderStage.PostProcessing: return "EXSLPostProcessingVSInput";
            }
        }
    }
}
