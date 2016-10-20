using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using LinqExpression = System.Linq.Expressions.Expression;

namespace Maze
{
    public class TypeFactory
    {
        private static readonly Lazy<ModuleBuilder> moduleBuilder = new Lazy<ModuleBuilder>(CreateModuleBuilder);

        private static int typeIndex;

        public NewExpression CreateProxy(IReadOnlyCollection<LinqExpression> arguments, IReadOnlyList<string> members)
        {
            var type = this.BuildType(arguments.Zip(members, (argument, member) => new KeyValuePair<string, Type>(member, argument.Type)));

            return LinqExpression.New(type.GetConstructors().Single(), arguments, members.Select(name => type.GetProperty(name)));
        }

        public Type BuildType(IEnumerable<KeyValuePair<string, Type>> members)
        {
            var baseType = typeof(DynamicProxy);

            TypeBuilder typeBuilder = moduleBuilder.Value.DefineType("DynamicProxy_" + Interlocked.Increment(ref typeIndex), TypeAttributes.Public, baseType);

            var fields = members.Select(member => typeBuilder.DefineField("__" + member.Key, member.Value, FieldAttributes.Private)).ToArray();

            var objCtor = baseType.GetConstructor(Type.EmptyTypes);

            var ctorParams = fields.Select(x => x.FieldType).ToArray();

            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public, CallingConventions.Standard, ctorParams);

            var ctorIL = ctorBuilder.GetILGenerator();

            ctorIL.Emit(OpCodes.Ldarg_0);
            ctorIL.Emit(OpCodes.Call, objCtor);

            for (int index = 0; index < fields.Length; index++)
            {
                ctorIL.Emit(OpCodes.Ldarg_0);

                switch (index + 1)
                {
                    case 1:
                        ctorIL.Emit(OpCodes.Ldarg_1);
                        break;
                    case 2:
                        ctorIL.Emit(OpCodes.Ldarg_2);
                        break;
                    case 3:
                        ctorIL.Emit(OpCodes.Ldarg_3);
                        break;
                    default:
                        ctorIL.Emit(OpCodes.Ldarg_S, index + 1);
                        break;
                }

                ctorIL.Emit(OpCodes.Stfld, fields[index]);

                var name = fields[index].Name.Substring(2);

                var propBuilder = typeBuilder.DefineProperty(name, PropertyAttributes.HasDefault, fields[index].FieldType, null);

                var getterBuilder = typeBuilder.DefineMethod(
                    "get_" + name,
                    MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig,
                    fields[index].FieldType,
                    Type.EmptyTypes);

                var getterIL = getterBuilder.GetILGenerator();

                getterIL.Emit(OpCodes.Ldarg_0);
                getterIL.Emit(OpCodes.Ldfld, fields[index]);
                getterIL.Emit(OpCodes.Ret);

                propBuilder.SetGetMethod(getterBuilder);
            }

            ctorIL.Emit(OpCodes.Ret);

            return typeBuilder.CreateTypeInfo().AsType();
        }

        private static ModuleBuilder CreateModuleBuilder()
        {
            var assembluBuilder = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("MazeDynamicAssembly"), AssemblyBuilderAccess.Run);

            return assembluBuilder.DefineDynamicModule("MazeDynamicModule");
        }

        public class DynamicProxy
        {
        }
    }
}
