using System.Text.RegularExpressions;

namespace ExtremeEngine.HLSLTools
{
    /// <summary> ExtremeEngine intermediate shader language(EXISL).
    /// EXISL is used to handle a shader that is not compiled to a legacy HLSL and have some special ExtremeEngine properties.</summary>
    public static partial class EXSLANG
    {

        public const int EXISLVersionMajor = 1, EXISLVersionMinor = 0, EXISLVersionPatch = 0;
        public static readonly Version EXISLVersion = new(EXISLVersionMajor, EXISLVersionMinor, EXISLVersionPatch);

        public const string ILFormat = ".exisl",
            ILKeyword = "$IL",
            ILVersionKeyword = "ILVersion",
            ILDefineKeyword = "Define",
            ILPropertyStart = "@IL.",
            ILAttributeStart = "@IL",

            ILNodeKey = "Node",
            ILCommentNodeKey = "CommentNode",
            ILNodePropertiesKey = "NodeProperties";

        public struct EXISLProperties
        {

            public readonly HLSLfloat2 Position, Scale = new(1, 1);

            public EXISLProperties(HLSLfloat2 position, HLSLfloat2 scale)
            {
                Position = position; Scale = scale;
            }
        }

        public struct EXISLNode
        {

            public readonly HLSLMethod Method;
            public readonly EXISLProperties Properties;

            public EXISLNode(EXISLProperties? properties, HLSLMethod method)
            {
                if (properties != null)
                    Properties = properties.Value;
                else
                    Properties = new(new(0, 0), new(1, 1));

                Method = method;
            }
        }

        /// <summary> HLSL writer with EXSL ExtremeEngine extension support.</summary>
        public partial class HLSLWriter
        {

            /// <summary> Define if need to write a EXISL(IL) file or a regular HLSL.</summary>
            public readonly bool IsIL;

            public static readonly Version ILVersion = EXISLVersion;

            public void IL_DefineEXISLShader(bool newLineOnEnd = true)
            {
                if (ThrowErrorIfNotIL())
                    return;

                Write(ILKeyword);
                EndLineWithoutNewLine();
                Comment("Defines that the file is in the ExtremeEngine intermediate shader language(EXISL) format.", newLineOnEnd);
            }

            public void IL_DefineEXISLVersion(bool endLine = true)
            {
                if (ThrowErrorIfNotIL())
                    return;

                IL_Define(ILVersionKeyword + "(" + '"' + EXISLVersion.ToString() + '"' + ")");
                if (endLine)
                    EndLine();
            }

            public void IL_StartNodeMethod(bool _static = false, EXISLProperties? properties = null, string name = "main", params HLSLParameter[] parameters) => IL_StartNodeMethod(_static, properties, name, parameters);
            public void IL_StartNodeMethod<T>(bool _static = false, EXISLProperties? properties = null, string name = "main", params HLSLParameter[] parameters) where T : HLSLType => IL_StartNodeMethod(_static, properties, Activator.CreateInstance<T>(), name, parameters);
            public void IL_StartNodeMethod(bool _static, EXISLProperties? properties, HLSLType? returnHLSLType, string name = "main", params HLSLParameter[] parameters)
            {
                if (ThrowErrorIfNotIL())
                    return;

                if (properties != null)
                    IL_NodePropertiesAttribute(properties.Value);

                if (_static)
                    Write(StaticKeyword + ' ');

                IL_Write(ILNodeKey + ' ');

                StartMethod(false, returnHLSLType, name, parameters);
            }

            public void IL_DefineCommentNode(EXISLProperties? properties, string? header, string comment)
            {
                if (ThrowErrorIfNotIL())
                    return;

                if (properties != null)
                    IL_NodePropertiesAttribute(properties.Value);

                IL_Write(ILCommentNodeKey);

                Write("(" + '"' + header + '"' + ", " + '"' + comment + '"' + ")");
                EndLine();
            }

            public void IL_Define(string str)
            {
                if (ThrowErrorIfNotIL())
                    return;

                IL_Write(ILDefineKeyword + ' ' + str);
            }

            public void IL_NodePropertiesAttribute(EXISLProperties properties, bool newLineOnEnd = true)
            {
                if (ThrowErrorIfNotIL())
                    return;

                Write(ILAttributeStart);
                DefineAttribute(ILNodePropertiesKey, newLineOnEnd, HLSLTools.HLSL.CreateHLSLParameter<HLSLfloat2>("position"), HLSLTools.HLSL.CreateHLSLParameter<HLSLfloat2>("scale"));
            }

            public void IL_Write(string str)
            {
                if (ThrowErrorIfNotIL())
                    return;

                Write(ILPropertyStart + str);
            }

            /// <summary> Return true if not IL and throws a error.</summary>
            public bool ThrowErrorIfNotIL()
            {
                if (!IsIL)
                {
                    Debug.LogError("Cannot use a IL method when the writer is not set to IL!");
                    return true;
                }
                return false;
            }
        }

        /// <summary> HLSL reader with EXSL ExtremeEngine extension support.</summary>
        public partial class HLSLReader
        {

            /// <summary> Returns true if it's EXISL file.</summary>
            public bool GetIsIL()
            {
                if (CurrentLine == ILKeyword)
                    return true;
                return false;
            }

            /// <summary> Returns true if it's EXISL file.</summary>
            public Version? GetILVersion()
            {
                string pattern = ILVersionKeyword + @"\(\""(?<version>\d+\.\d+\.\d+)\""\)";
                var match = Regex.Match(CurrentLine, pattern);
                if (match.Success)
                    return new(match.Groups["version"].Value);

                return null;
            }

            /// <summary> Get's IL node(IL method) and gives the result.</summary>
            public string? GetILNode(out EXISLProperties? properties, out HLSLType? outType, out string? methodName, out IEnumerable<HLSLParameter>? inParameters)
            {
                properties = null; outType = null; methodName = null; inParameters = null;

                if (CurrentLine.StartsWith(ILPropertyStart + ILNodeKey))
                {
                    //string pattern = @"@(?<ilNode>\w+)\s+(?<returnType>\w+)\s+(?<methodName>\w+)\((?<parameters>[^\)]+)\)";
                    return GetMethod(out outType, out methodName, out inParameters);
                }
                return null;
            }
        }
    }
}
