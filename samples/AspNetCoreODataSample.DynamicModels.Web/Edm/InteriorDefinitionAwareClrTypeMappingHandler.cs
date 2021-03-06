﻿using System;
using System.Linq;
using AspNetCoreODataSample.DynamicModels.Web.Models;
using Microsoft.AspNet.OData;
using Microsoft.OData.Edm;
using Microsoft.OData.UriParser;
using ODataPath = Microsoft.AspNet.OData.Routing.ODataPath;

namespace AspNetCoreODataSample.DynamicModels.Web.Edm
{
    public class InteriorDefinitionAwareClrTypeMappingHandler : IEdmModelClrTypeMappingHandler
    {
        private static readonly Type InteriorType = typeof(Interior);

        public IEdmType MapClrTypeToEdmType(IEdmModel edmModel, Type clrType)
        {
            // when the base type is requested, provide the base EDM type
            if (clrType == InteriorType)
            {
                return edmModel.FindDeclaredType(InteriorType.FullName);
            }
            return null;
        }

        public IEdmTypeReference MapClrTypeToTypeReference(IEdmModel edmModel, Type clrType, ODataPath path)
        {
            if (!(path.Segments.FirstOrDefault() is EntitySetSegment entitySetSegment))
            {
                // if there is no entity set requested, we cannot resolve the EDM type to use 
                return null;
            }

            // unwrap which entity was requested
            var collectionType = (IEdmCollectionType)entitySetSegment.EntitySet.Type;
            var elementType = collectionType.ElementType.Definition;
            switch (elementType.TypeKind)
            {
                case EdmTypeKind.Entity:
                    return new EdmEntityTypeReference((IEdmEntityType) elementType, true);
                case EdmTypeKind.Complex:
                    return new EdmComplexTypeReference((IEdmComplexType) elementType, true);
            }

            return null;
        }

        public IEdmCollectionType MapClrEnumerableToEdmCollection(IEdmModel edmModel, Type clrType, Type elementClrType)
        {
            // when the base type is requested, provide the base EDM type
            if (elementClrType == InteriorType)
            {
                return new EdmCollectionType(new EdmEntityTypeReference((IEdmEntityType)edmModel.FindDeclaredType(InteriorType.FullName), true));
            }
            return null;
        }

        public IEdmType MapClrInstanceToEdmType(IEdmModel edmModel, object clrInstance)
        {
            // simply unwrap the type reference (Dont-Repeat-Yourself)
            return MapClrInstanceToEdmTypeReference(edmModel, clrInstance)?.Definition;
        }

        public IEdmTypeReference MapClrInstanceToEdmTypeReference(IEdmModel edmModel, object clrInstance)
        {
            // handle IQueryable wrappers
            if (clrInstance is IEdmTypedIQueryableWrapper wrapper)
            {
                return new EdmCollectionTypeReference(wrapper.EdmCollectionType);
            }

            // handle IQueryable wrappers in SingleResult
            if (clrInstance is SingleResult<Interior> single && single.Queryable is IEdmTypedIQueryableWrapper singleWrapper)
            {
                return new EdmCollectionTypeReference(singleWrapper.EdmCollectionType);
            }

            // handle Interior instances
            if (clrInstance is Interior interior)
            {
                // lookup IEdmEntityType with matching definition (via annotation)
                var type = edmModel.SchemaElements
                    .OfType<IEdmEntityType>()
                    .Select(edmType => new { EdmType = edmType, Annotation = edmModel.GetAnnotationValue<InteriorDefinitionAnnotation>(edmType) })
                    .Where(tuple => tuple.Annotation != null && tuple.Annotation.DefinitionID == interior.DefinitionID)
                    .Select(tuple => tuple.EdmType)
                    .SingleOrDefault();
                if (type != null)
                {
                    return new EdmEntityTypeReference(type, true);
                }
            }
            return null;
        }

        public void InitializeClrInstanceForDeserialization(IEdmModel edmModel, IEdmStructuredTypeReference typeReference,
            object clrInstance)
        {
            InteriorDefinitionAnnotation annotation;
            if (clrInstance is Interior interior &&
                (annotation = edmModel.GetAnnotationValue<InteriorDefinitionAnnotation>(typeReference.Definition)) != null)
            {
                interior.DefinitionID = annotation.DefinitionID;
            }
        }
    }
}
