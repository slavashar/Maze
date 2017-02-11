using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace DataFlow.Infrastucture
{
    public static class ChinookContextExtension
    {
        public static void PopulateContext(this ChinookContext context)
        {
            var assembly = context.GetType().GetTypeInfo().Assembly;

            var resourceStream = assembly.GetManifestResourceStream("DataFlow.ChinookData.xml");
            
            using (var stream = new StreamReader(resourceStream, Encoding.UTF8))
            {
                using (var reader = XmlReader.Create(stream))
                {
                    PopulateContext(context, reader);
                }
            }
        }

        public static void PopulateContext(this DbContext context, XmlReader reader)
        {
            var parsers = new Dictionary<string, ElementReader>();

            reader.ReadStartElement();

            while (reader.MoveToContent() == XmlNodeType.Element)
            {
                if (!parsers.TryGetValue(reader.Name, out ElementReader parser))
                {
                    parsers.Add(reader.Name, parser = ElementReader.Create(context, reader.Name));
                }

                reader.ReadStartElement();
                parser.Process(reader);
                reader.ReadEndElement();
            }
        }

        private abstract class ElementReader
        {
            public static ElementReader Create(DbContext context, string name)
            {
#if NET451
                var property = context.GetType().GetProperty(name);

                var element = property.PropertyType.GetGenericArguments().Single();
#else
                var property = context.GetType().GetRuntimeProperty(name);

                var element = property.PropertyType.GetType().GenericTypeArguments.Single();
#endif

                return (ElementReader)Activator.CreateInstance(
                    typeof(ElementReader<>).MakeGenericType(element),
                    new object[] { property.GetValue(context) });
            }

            public abstract void Process(XmlReader reader);

            protected static Expression GetExpression(ParameterExpression reader, Type type, string name)
            {
                Delegate read;

                if (type == typeof(string))
                {
                    read = new Func<XmlReader, string, string>(ReadString);
                }
                else if (type == typeof(int))
                {
                    read = new Func<XmlReader, string, int>(ReadInt);
                }
                else if (type == typeof(DateTime))
                {
                    read = new Func<XmlReader, string, DateTime>(ReadDateTime);
                }
                else if (type == typeof(decimal))
                {
                    read = new Func<XmlReader, string, decimal>(ReadDecimal);
                }
                else
                {
                    throw new InvalidOperationException("Unknown type: " + type.Name);
                }

                return Expression.Invoke(Expression.Constant(read), reader, Expression.Constant(name));
            }

            private static string ReadString(XmlReader reader, string elementName) => Read(reader, elementName, r => r.ReadContentAsString());

            private static int ReadInt(XmlReader reader, string elementName) => Read(reader, elementName, r => r.ReadContentAsInt());

            private static DateTime ReadDateTime(XmlReader reader, string elementName) => Read(reader, elementName, r => r.ReadContentAsDateTimeOffset().DateTime);

            private static decimal ReadDecimal(XmlReader reader, string elementName) => Read(reader, elementName, r => r.ReadContentAsDecimal());

            private static TResult Read<TResult>(XmlReader reader, string elementName, Func<XmlReader, TResult> read)
            {
                reader.ReadStartElement(elementName);
                var result = read(reader);
                reader.ReadEndElement();
                return result;
            }
        }

        private class ElementReader<T> : ElementReader
            where T : class
        {
            private readonly DbSet<T> set;
            private readonly Func<XmlReader, T> read;

            public ElementReader(DbSet<T> set)
            {
                this.set = set;

                var reader = Expression.Parameter(typeof(XmlReader), "reader");
#if NET451
                var bindings = typeof(T).GetProperties()
#else
                var bindings = typeof(T).GetType().GetRuntimeProperties()
#endif
                    .OrderBy(x => x.MetadataToken).Select(x => Expression.Bind(x, GetExpression(reader, x.PropertyType, x.Name)));

                var lambda = Expression.Lambda<Func<XmlReader, T>>(Expression.MemberInit(Expression.New(typeof(T)), bindings), reader);

                this.read = lambda.Compile();
            }

            public override void Process(XmlReader reader)
            {
                var item = this.read(reader);
                this.set.Add(item);
            }
        }
    }
}
