﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace ModestTree.Zenject
{
    // Iterate over fields/properties on a given object and inject any with the [Inject] attribute
    public class FieldsInjecter
    {
        public static void Inject(DiContainer container, object injectable)
        {
            Inject(container, injectable, Enumerable.Empty<object>());
        }

        public static void Inject(DiContainer container, object injectable, IEnumerable<object> additional)
        {
            Inject(container, injectable, additional, false);
        }

        public static void Inject(DiContainer container, object injectable, IEnumerable<object> additional, bool shouldUseAll)
        {
            Assert.That(injectable != null);

            var fields = InjectionInfoHelper.GetFieldDependencies(injectable.GetType());

            var parentDependencies = new List<Type>(container.LookupsInProgress);

            var additionalCopy = additional.ToList();

            foreach (var fieldInfo in fields)
            {
                var injectInfo = InjectionInfoHelper.GetInjectInfo(fieldInfo);
                Assert.That(injectInfo != null);

                bool foundAdditional = false;
                foreach (object obj in additionalCopy)
                {
                    if (fieldInfo.FieldType.IsAssignableFrom(obj.GetType()))
                    {
                        fieldInfo.SetValue(injectable, obj);
                        additionalCopy.Remove(obj);
                        foundAdditional = true;
                        break;
                    }
                }

                if (foundAdditional)
                {
                    continue;
                }

                var context = new ResolveContext()
                {
                    Target = injectable.GetType(),
                    FieldName = fieldInfo.Name,
                    Identifier = injectInfo.Identifier,
                    Parents = parentDependencies,
                    TargetInstance = injectable,
                };

                var valueObj = ResolveField(container, fieldInfo, context, injectInfo, injectable);

                fieldInfo.SetValue(injectable, valueObj);
            }

            if (shouldUseAll && !additionalCopy.IsEmpty())
            {
                throw new ZenjectResolveException(
                    "Passed unnecessary parameters when injecting into type '{0}'. \nExtra Parameters: {1}\nObject graph:\n{2}",
                        injectable.GetType().GetPrettyName(),
                        String.Join(",", additionalCopy.Select(x => x.GetType().GetPrettyName()).ToArray()),
                        container.GetCurrentObjectGraph());
            }
        }

        static object ResolveField(
            DiContainer container,
            FieldInfo fieldInfo, ResolveContext context,
            InjectInfo injectInfo, object injectable)
        {
            var desiredType = fieldInfo.FieldType;

            if (container.HasBinding(desiredType, context))
            {
                return container.Resolve(desiredType, context);
            }

            // Dependencies that are lists are only optional if declared as such using the inject attribute
            bool isOptional = (injectInfo == null ? false : injectInfo.Optional);

            // If it's a list it might map to a collection
            if (ReflectionUtil.IsGenericList(desiredType))
            {
                var subType = desiredType.GetGenericArguments().Single();

                return container.ResolveMany(subType, context, isOptional);
            }

            if (!isOptional)
            {
                throw new ZenjectResolveException(
                    "Unable to find field with type '{0}' when injecting dependencies into '{1}'. \nObject graph:\n {2}",
                    fieldInfo.FieldType, injectable, container.GetCurrentObjectGraph());
            }

            return null;
        }
    }
}