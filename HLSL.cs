using System.Reflection;

namespace ExtremeEngine.HLSLTools
{

    public struct HLSLMethod
    {

        public readonly string Name = "main";
        public readonly HLSLType? Out = null;
        public readonly IEnumerable<HLSLParameter>? In = null;

        public HLSLMethod(string name, HLSLType? _out, params IEnumerable<HLSLParameter>? _in)
        {
            Name = name; Out = _out; In = _in;
        }
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class HLSLTypeAttribute : Attribute
    {

        public readonly string TypeName;

        public HLSLTypeAttribute(string typeName)
        {
            TypeName = typeName;
        }
    }

    public abstract class HLSLType
    {
        public string Name;
        public dynamic Data;

        public HLSLType(string name, dynamic data) { Name = name; Data = data; }
    }

    public abstract class HLSLType<T> : HLSLType
    {
        public T Value => Data;

        public HLSLType(T value) : base("null", value)
        {
            var attr = GetType().GetCustomAttribute<HLSLTypeAttribute>();
            if (attr != null)
            {
                Name = attr.TypeName;
                Data = value;
            }
            else
                Debug.LogError("HLSLType is not valid because it's not have a HLSLTypeAttribute and his name is not defined!");
        }
        public HLSLType(string typeName, T value) : base(typeName, value) { }

        public virtual void SetValue(T value)
        {
            Data = value;
        }

        public virtual T GetValue()
        {
            return Data;
        }
    }

    public class HLSLUnregisteredType : HLSLType<object?>
    {
        public HLSLUnregisteredType(string typeName, object? value) : base(typeName, value) { }
    }

    public class HLSLNULL : HLSLType<HLSLuint>
    {
        public HLSLNULL() : base("NULL", new(0)) { }
    }

    public class HLSLAttribute
    {
        public readonly string Name;

        public HLSLAttribute(string name) { Name = name; }
    }

    public struct HLSLField
    {
        public readonly HLSLParameter Parameter;
        public readonly bool Const = false, Static = false;
        public readonly string? Semantic;
        public readonly HLSLAttribute[] Attributes;

        public HLSLField(HLSLParameter parameter, bool _const = false, bool _static = false, string? semantic = null, params HLSLAttribute[] attributes)
        {
            Parameter = parameter; Const = _const; Static = _static; Semantic = semantic; Attributes = attributes;
        }
    }

    public struct HLSLParameter
    {
        public readonly HLSLType Type;
        public readonly string Name;

        public HLSLParameter(HLSLType type, string name)
        {
            Type = type; Name = name;
        }
    }

    public abstract class HLSLStruct : HLSLType<HLSLField[]>
    {
        public HLSLStruct(string typeName, HLSLField[] value) : base(typeName, value) { }
    }

    public class EXSLFragVSInput : HLSLStruct
    {
        //HLSLField[] Fields = new HLSLField[1] { HLSL.CreateHLSLField<HLSLfloat3>("color") };

        public EXSLFragVSInput() : base("EXSLFragVSInput", null) { }
    }

    public static class HLSL
    {
        public enum SyntaxStyle
        {
            /// <summary> HLSL with C syntax style(default for HLSL).</summary>
            C,
            /// <summary> HLSL with C++11 syntax style.</summary>
            CPP11,
        };

        public static string GetAttributeStart(HLSL.SyntaxStyle syntax)
        {
            if (syntax == HLSL.SyntaxStyle.C)
                return EXSLANG.AttributeStartKeyC;
            return EXSLANG.AttributeStartKeyCPP11;
        }

        public static string GetAttributeEnd(HLSL.SyntaxStyle syntax)
        {
            if (syntax == HLSL.SyntaxStyle.C)
                return EXSLANG.AttributeEndKeyC;
            return EXSLANG.AttributeEndKeyCPP11;
        }

        public static HLSLParameter CreateHLSLParameter<T>(string name) where T : HLSLType => new(Activator.CreateInstance<T>(), name);
        public static HLSLParameter CreateHLSLParameter(HLSLType type, string name) => new(type, name);
        public static HLSLField CreateHLSLField<T>(string name, bool _const = false, bool _static = false, string? semantic = null, params HLSLAttribute[] attributes) where T : HLSLType => new(CreateHLSLParameter<T>(name), _const, _static, semantic, attributes);

        /// <summary> Creates a type from a name if type not found it's will be typeof HLSLUnregisteredType! and not null</summary>
        public static HLSLType CreateTypeFromName(string typeName)
        {
            Type[] types = Assembly.GetExecutingAssembly().GetTypes();
            for (int i = 0; i < types.Length; i++)
            {
                if (types[i].IsAssignableTo(typeof(HLSLType)) && !types[i].IsAbstract)
                {
                    var attr = types[i].GetCustomAttribute<HLSLTypeAttribute>();
                    if (attr != null && attr.TypeName == typeName)
                        try
                        {
                            return Activator.CreateInstance(types[i]) as HLSLType;
                        }
                        catch (Exception ex)
                        {
                            Debug.Log("Cannot create HLSLType from name error: " + ex);
                        }
                }
            }
            return new HLSLUnregisteredType(typeName, null);
        }
    }

    [HLSLType("bool")]
    public class HLSLbool : HLSLType<bool>
    {
        public HLSLbool(bool value) : base(value) { }
    }

    [HLSLType("int")]
    public class HLSLint : HLSLType<Int32>
    {
        public Int32 value;

        public HLSLint(Int32 value) : base(value) { }
    }

    [HLSLType("uint")]
    public class HLSLuint : HLSLType<UInt32>
    {
        public HLSLuint(UInt32 value) : base(value) { }
    }

    [HLSLType("half")]
    public class HLSLhalf : HLSLType<Half>
    {
        public HLSLhalf(Half value) : base(value) { }
    }

    [HLSLType("float")]
    public class HLSLfloat : HLSLType<float>
    {
        public HLSLfloat(float value) : base(value) { }
    }

    [HLSLType("double")]
    public class HLSLdouble : HLSLType<double>
    {
        public HLSLdouble(double value) : base(value) { }
    }

    [HLSLType("float2")]
    public class HLSLfloat2 : HLSLType<Tuple<float, float>>
    {
        public HLSLfloat2() : this(0, 0) { }
        public HLSLfloat2(float x) : this(x, 0) { }
        public HLSLfloat2(float x, float y) : base(new(x, y)) { }
    }

    [HLSLType("float3")]
    public class HLSLfloat3 : HLSLType<Tuple<float, float, float>>
    {

        public HLSLfloat3() : this(0, 0, 0) { }
        public HLSLfloat3(float x) : this(x, 0, 0) { }
        public HLSLfloat3(float x, float y) : this(x, y, 0) { }
        public HLSLfloat3(float x, float y, float z) : base(new(x, y, z)) { }
    }

    [HLSLType("float4")]
    public class HLSLfloat4 : HLSLType<Tuple<float, float, float, float>>
    {
        public HLSLfloat4() : this(0, 0, 0) { }
        public HLSLfloat4(float x) : this(x, 0, 0) { }
        public HLSLfloat4(float x, float y) : this(x, y, 0) { }
        public HLSLfloat4(float x, float y, float z) : this(x, y, z, 0) { }
        public HLSLfloat4(float x, float y, float z, float w) : base(new(x, y, z, w)) { }
    }

    public struct HLSLArrayStruct<T>
    {
        public readonly HLSLType<T> Type;
        public readonly int Size;

        public HLSLArrayStruct(HLSLType<T> type, int size) { Type = type; Size = size; }
    }

    public class HLSLArray<T> : HLSLType<HLSLArrayStruct<T>> where T : HLSLType<T>
    {

        public HLSLArray(T type, int size) : base("array", new HLSLArrayStruct<T>(type, size)) { }
    }
}
