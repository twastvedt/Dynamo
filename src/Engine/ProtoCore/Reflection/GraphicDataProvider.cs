﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Autodesk.DesignScript.Interfaces;
using Autodesk.DesignScript.Runtime;
using ProtoCore.DSASM;
using ProtoCore.Utils;

namespace ProtoCore.Mirror
{
    class GraphicDataProvider : IGraphicDataProvider
    {
        private static Dictionary<Assembly, IGraphicDataProvider> mDataProviders =
            new Dictionary<Assembly, IGraphicDataProvider>();

        private static Dictionary<System.Type, IGraphicDataProvider> mGraphicTypes =
            new Dictionary<System.Type, IGraphicDataProvider>();

        public List<IGraphicItem> GetGraphicItems(object obj)
        {
            return GetGraphicItemsFromObject(obj);
        }

        public static List<IGraphicItem> GetGraphicItemsFromObject(object obj)
        {
            if(null == obj)
                return null;
            
            List<IGraphicItem> items = new List<IGraphicItem>();
            System.Type objType = obj.GetType();
            IEnumerable collection = obj as IEnumerable;
            if (null != collection)
            {
                foreach (var item in collection)
                {
                    try
                    {


                        List<IGraphicItem> graphics = GetGraphicItemsFromObject(item);
                        if (null != graphics)
                            items.AddRange(graphics);
                    }
                    catch (Exception e)
                    {
                        System.Diagnostics.Debug.WriteLine("GetGraphicItems: " + e);
                    }
                }
                return items;
            }

            IGraphicItem graphicItem = obj as IGraphicItem;
            if (graphicItem != null)
            {
                items.Add(graphicItem);
                return items;
            }

            IGraphicDataProvider dataProvider = GetGraphicDataProviderForType(objType);
            if (null == dataProvider)
            {
                dataProvider = new GraphicObjectType(objType);
                mGraphicTypes.Add(objType, dataProvider);
            }

            return dataProvider.GetGraphicItems(obj);
        }

        public static IGraphicDataProvider GetGraphicDataProviderForType(System.Type objType)
        {
            IGraphicDataProvider dataProvider = null;
            if (mGraphicTypes.TryGetValue(objType, out dataProvider))
                return dataProvider;

            //Get data provider from the assembly of the type
            System.Type type = objType;
            while (dataProvider == null)
            {
                dataProvider = GetDataProviderFromAssembly(type);
                if (null != dataProvider)
                {
                    mGraphicTypes.Add(objType, dataProvider);
                    return dataProvider;
                }

                type = type.BaseType;
                if (type == null)
                    break;
            }

            return null;
        }

        private static IGraphicDataProvider GetDataProviderFromAssembly(System.Type type)
        {
            //Check if we already have a provider for the type.Assembly
            IGraphicDataProvider provider = null;
            if (mDataProviders.TryGetValue(type.Assembly, out provider))
                return provider;

            mDataProviders.Add(type.Assembly, null); //initialize with no dataprovider from this type assembly

            //Check if this assembly implements IGraphicDataProvider interface
            var providerType = ProtoFFI.CLRDLLModule.GetImplemetationType(type.Assembly, typeof(IGraphicDataProvider), typeof(GraphicDataProviderAttribute), false);
            if (providerType == null)
                return null;

            //Got the type of IGraphicDataProvider interface implementation
            //Create an instance of IGraphicDataProvider using default constructor.
            provider = (IGraphicDataProvider)Activator.CreateInstance(providerType, true);
            if (null != provider)
                mDataProviders[type.Assembly] = provider;

            return provider;
        }

        public void Tessellate(List<object> objects, IRenderPackage package, TessellationParameters parameters)
        {
            foreach (var item in objects)
            {
                List<IGraphicItem> graphicItems = GetGraphicItems(item);
                if (null == graphicItems || graphicItems.Count == 0)
                    continue;

                foreach (var g in graphicItems)
                {
                    g.Tessellate(package, parameters);
                }
            }
        }

        internal List<IGraphicItem> GetGraphicItems(DSASM.StackValue svData, RuntimeCore runtimeCore)
        {
            Validity.Assert(svData.IsPointer);

            object obj = GetCLRObject(svData, runtimeCore);
            if (obj != null)
                return GetGraphicItems(obj);

            return null;
        }

        //Store marshaller for repeated calls to GetCLRObject.  Used for short-circuit of re-allocation of marshaller / interpreter object.
        private static Interpreter interpreter;
        private static ProtoFFI.FFIObjectMarshaler marshaler;
        
        internal object GetCLRObject(StackValue svData, RuntimeCore runtimeCore)
        {
            if (null == runtimeCore.DSExecutable.classTable)
                return null;

            //The GetCLRObject function is typically utilized to retrieve a ClrObject from a StackValue of type pointer.
            //There is an edge cases for pointers where the pointer references a non CLR object.  This code
            //checks for this edge case by verifying that the requested StackValue pointer is associated with an
            //imported library.  An example is the "Function" pointer which does not have an associated CLRObject.
            //In that case, the return value should be null.
            var classNode = runtimeCore.DSExecutable.classTable.GetClassNodeAtIndex(svData.metaData.type);
            if (classNode != null  && !classNode.IsImportedClass)
            {
                return null;
            }

            if (marshaler != null)
            {
                return marshaler.UnMarshal(svData, null, interpreter, typeof(object));
            }

            try
            {
                interpreter = new ProtoCore.DSASM.Interpreter(runtimeCore, false);
                var helper = ProtoFFI.DLLFFIHandler.GetModuleHelper(ProtoFFI.FFILanguage.CSharp);
                marshaler = helper.GetMarshaler(runtimeCore);
                return marshaler.UnMarshal(svData, null, interpreter, typeof(object));
            }
            catch (System.Exception)
            {
                marshaler = null;
                return null;
            }
        }

        internal static void ClearMarshaller()
        {
            interpreter = null;
            marshaler = null;
        }
    }

    /// <summary>
    /// This class reflects on properties of a given type and finds graphic data
    /// provider for them. If any of the property type has graphic data provider
    /// it gets the graphic items from those properties. It doesn't query indexed
    /// properties only uses public properties.
    /// </summary>
    class GraphicObjectType : IGraphicDataProvider
    {
        private List<PropertyInfo> mGraphicProperties;
        public GraphicObjectType(System.Type type)
        {
            mGraphicProperties = GetGraphicProperties(type);
        }

        private List<PropertyInfo> GetGraphicProperties(System.Type type)
        {
            List<PropertyInfo> graphicProperties = new List<PropertyInfo>();
            PropertyInfo[] properties = type.GetProperties();
            foreach (var item in properties)
            {
                //Check if we have a data provider for this property type.
                IGraphicDataProvider provider = GraphicDataProvider.GetGraphicDataProviderForType(item.PropertyType);
                if (null != provider)
                    graphicProperties.Add(item);
            }

            return graphicProperties;
        }

        public List<IGraphicItem> GetGraphicItems(object obj)
        {
            if (mGraphicProperties.Count <= 0)
                return null;

            List<IGraphicItem> graphics = new List<IGraphicItem>();
            foreach (var item in mGraphicProperties)
            {
                var property = item.GetValue(obj, null); //For now indexed property is not queried
                if (null != property)
                {
                    List<IGraphicItem> items = GraphicDataProvider.GetGraphicItemsFromObject(property);
                    if(null != items && items.Count > 0)
                        graphics.AddRange(items);
                }
            }
            if (graphics.Count > 0)
                return graphics;

            return null;
        }

        public void Tessellate(List<object> objects, IRenderPackage package, TessellationParameters parameters)
        {
            throw new NotImplementedException();
        }
    }

}
