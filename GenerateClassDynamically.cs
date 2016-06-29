using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Web;

namespace Salus.Esocial.Areas
{
    public class GenerateClassDynamically
    {
        public object CreateObject(Object value, string nameClass)
        {
            object bvDynamically = CreateNewObject(value, nameClass);

            return bvDynamically;
        }

        #region Create Class
        private object CreateNewObject(Object value, string nameClass)
        {
            Type myType = CompileResultType(value, nameClass);
            object bvDynamically = Activator.CreateInstance(myType);

            //Population properties with values var
            foreach (var propertyBVDynamically in bvDynamically.GetType().GetProperties())
            {
                foreach (var property in value.GetType().GetProperties())
                {
                    if (propertyBVDynamically.Name.Equals(property.Name))
                    {
                        try
                        {
                            var propertyValue = property.GetValue(value, null).ToString();
                            propertyBVDynamically.SetValue(bvDynamically, value, null);
                            break;
                        }
                        catch (Exception) { }
                    }
                }
            }
            return bvDynamically;
        }

        private TypeBuilder GetTypeBuilder(string nameClass)
        {
            var typeSignature = nameClass;
            var an = new AssemblyName(typeSignature);

            AssemblyBuilder assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(an, AssemblyBuilderAccess.Run);
            ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("MainModule");

            TypeBuilder tb = moduleBuilder.DefineType(typeSignature
                                , TypeAttributes.Public |
                                TypeAttributes.Class |
                                TypeAttributes.AutoClass |
                                TypeAttributes.AnsiClass |
                                TypeAttributes.BeforeFieldInit |
                                TypeAttributes.AutoLayout
                                , null);
            return tb;
        }
        #endregion

        #region Create Property Class Dynamically

        private Type CompileResultType(Object value, string nameClass)
        {
            TypeBuilder tb = GetTypeBuilder(nameClass);
            ConstructorBuilder constructor = tb.DefineDefaultConstructor(MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName);

            PropertyInfo[] properties = value.GetType().GetProperties();

            foreach (var field in properties)
            {
                string propertyValue;
                try
                {
                    propertyValue = field.GetValue(value, null).ToString();
                    {
                        //selection criterion
                        if (validateValue(propertyValue)) 
                        {
                            CreateProperty(tb, field.Name, field.GetType());
                        }
                    }
                }
                catch (Exception) { }
            }

            Type objectType = tb.CreateType();
            return objectType;
        }

        private void CreateProperty(TypeBuilder tb, string propertyName, Type propertyType)
        {
            createGenericPropertyGet(tb, propertyName, propertyType);
            createGenericPropertySet(tb, propertyName, propertyType);
        }

        private void createGenericPropertyGet(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField(propertyName, propertyType, FieldAttributes.Public);
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            MethodBuilder getPropMthdBldr = tb.DefineMethod("get_" + propertyName, MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(string), Type.EmptyTypes);

            ILGenerator getIl = getPropMthdBldr.GetILGenerator();
            getIl.Emit(OpCodes.Ldarg_0);
            getIl.Emit(OpCodes.Ldfld, fieldBuilder);
            getIl.Emit(OpCodes.Ret);
            propertyBuilder.SetGetMethod(getPropMthdBldr);
        }

        private void createGenericPropertySet(TypeBuilder tb, string propertyName, Type propertyType)
        {
            FieldBuilder fieldBuilder = tb.DefineField(propertyName, propertyType, FieldAttributes.Public);
            PropertyBuilder propertyBuilder = tb.DefineProperty(propertyName, PropertyAttributes.HasDefault, propertyType, null);

            MethodBuilder setPropMthdBldr =
                tb.DefineMethod("set_" + propertyName,
                  MethodAttributes.Public |
                  MethodAttributes.SpecialName |
                  MethodAttributes.HideBySig,
                  null, new[] { propertyType });

            ILGenerator setIl = setPropMthdBldr.GetILGenerator();
            Label modifyProperty = setIl.DefineLabel();
            Label exitSet = setIl.DefineLabel();

            setIl.MarkLabel(modifyProperty);
            setIl.Emit(OpCodes.Ldarg_0);
            setIl.Emit(OpCodes.Ldarg_1);
            setIl.Emit(OpCodes.Stfld, fieldBuilder);

            setIl.Emit(OpCodes.Nop);
            setIl.MarkLabel(exitSet);
            setIl.Emit(OpCodes.Ret);

            propertyBuilder.SetSetMethod(setPropMthdBldr);
        }
        #endregion

        #region verify values NULL
        private bool validateValue(string value)
        {
            bool result = true;

            if (string.IsNullOrEmpty(value) || value.Equals("0") || value.Equals(DateTime.MinValue))
            {
                result = false;
            }

            return result;
        }
        #endregion

    }
}