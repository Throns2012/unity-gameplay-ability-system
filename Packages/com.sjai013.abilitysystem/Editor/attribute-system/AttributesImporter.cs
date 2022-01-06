using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.AssetImporters;

namespace GameplayAbilitySystem.AttributeSystem.Editor
{
    [ScriptedImporter(version, extension)]
    internal class AttributeImporter : ScriptedImporter
    {
        private const int version = 1;
        private const string extension = ".gasattr";
        public override void OnImportAsset(AssetImportContext ctx)
        {
            if (ctx == null)
                throw new ArgumentNullException(nameof(ctx));

            string text;
            try
            {
                text = File.ReadAllText(ctx.assetPath);
            }
            catch (Exception exception)
            {
                ctx.LogImportError($"Could not read file '{ctx.assetPath}' ({exception})");
                return;
            }

            var attributes = Deserialise(text);
            GenerateWrapperClass(ctx, attributes);

        }

        private AttributesFileSchema Deserialise(string json)
        {
            return JsonConvert.DeserializeObject<AttributesFileSchema>(json);

        }

        private void GenerateWrapperClass(AssetImportContext ctx, AttributesFileSchema attributes)
        {
            var assetPath = ctx.assetPath;
            // Create file at same location as imported asset
            var directory = Path.GetDirectoryName(assetPath);
            var fileName = Path.GetFileNameWithoutExtension(assetPath);
            var componentsFilePath = Path.Combine(directory, fileName) + "_Components.cs";
            var currentValueCalculationSystemFilePath = Path.Combine(directory, fileName) + "_CVUpdateSystem.cs";
            var attributeArchetypesFilePath = Path.Combine(directory, fileName) + "_AttributeArchetypes.cs";

            var sourceFileName = ctx.assetPath;
            var attributeStructs = GenerateAttributeStructs(attributes);

            var currentValueJobs = GenerateCurrentValueJobs(attributes);
            var currentValueSystem = GenerateCurrentValueSystem(attributes, currentValueJobs);

            var archetypeDict = CreateComponentArchetypeGroups(attributes);
            var archetypeClasses = GenerateAttributeArchetypeClasses(archetypeDict);

            WriteFile(componentsFilePath, attributeStructs);
            WriteFile(currentValueCalculationSystemFilePath, currentValueSystem);
            WriteFile(attributeArchetypesFilePath, archetypeClasses);
            // // Generate wrapper class starting at the inner most group for each attribute group
            // // Each asset represents a single attribute group


            // // Attributes
            // var attributeCsFragmentList = new List<string>();
            // for (var i = 0; i < deserializedObj.Attributes.Count; i++)
            // {
            //     var attributeCodeGenerator = new AttributeTemplate();
            //     var fragment = attributeCodeGenerator.Generate(deserializedObj.Attributes[i].AttributeDescription, deserializedObj.Attributes[i].AttributeName);
            //     attributeCsFragmentList.Add(fragment);
            // }

            // var attributesFragment = String.Join(Environment.NewLine, attributeCsFragmentList);

            // // Attribute Group
            // var attributeGroupCodeGen = new AttributeGroupTemplate();
            // var attributeGroupFragment = attributeGroupCodeGen.Generate(deserializedObj.Description, deserializedObj.Name, attributesFragment);

            // // Base Template
            // var baseTemplateCodeGen = new BaseTemplate();
            // var classFragment = baseTemplateCodeGen.Generate(version.ToString(), ctx.assetPath, deserializedObj.Namespace, attributeGroupFragment);

            // WriteFile(wrapperFilePath, classFragment);


            // UnityEditor.Compilation.CompilationPipeline.RequestScriptCompilation();
        }

        private static void WriteFile(string wrapperFilePath, string content)
        {

            File.WriteAllText(wrapperFilePath, content);
        }

        private string GenerateAttributeStructs(AttributesFileSchema schema)
        {
            var attributes = schema.Attributes;
            string[] fragmentsArr = new string[attributes.Length];
            for (var i = 0; i < attributes.Length; i++)
            {
                fragmentsArr[i] = new AttributeTemplate().Generate(attributes[i].Name, attributes[i].Description);
            }

            string fragments = String.Join(Environment.NewLine, fragmentsArr);
            return fragments;
        }

        private string GenerateCurrentValueJobs(AttributesFileSchema schema)
        {
            var attributes = schema.Attributes;
            string[] fragmentsArr = new string[attributes.Length];
            for (var i = 0; i < attributes.Length; i++)
            {
                fragmentsArr[i] = new JobTemplate().Generate(i, attributes[i].Name);
            }
            string fragments = String.Join(Environment.NewLine, fragmentsArr);
            return fragments;
        }

        private string GenerateCurrentValueSystem(AttributesFileSchema schema, string jobsFragment)
        {
            var fragment = new CVSystemTemplate().Generate(schema.Attributes.Length, schema.Name, jobsFragment);
            return fragment;
        }

        private Dictionary<string, List<string>> CreateComponentArchetypeGroups(AttributesFileSchema schema)
        {
            Dictionary<string, List<string>> componentGroups = new Dictionary<string, List<string>>(schema.Attributes.Length);
            for (var i = 0; i < schema.Attributes.Length; i++)
            {
                var groups = schema.Attributes[i].Groups;
                for (var g = 0; g < groups.Length; g++)
                {
                    var groupName = groups[g];
                    var attributeTypeName = schema.Attributes[i].Name;
                    if (componentGroups.TryGetValue(groupName, out var list))
                    {
                        list.Add(attributeTypeName);
                    }
                    else
                    {
                        componentGroups.Add(groupName, new List<string>() { attributeTypeName });
                    }
                }
            }

            return componentGroups;
        }

        private string GenerateAttributeArchetypeClasses(Dictionary<string, List<string>> archetypes)
        {
            List<string> classes = new List<string>(archetypes.Keys.Count);
            foreach (var entry in archetypes)
            {
                var @class = GenerateAttributeArchetypeClass(entry.Key, entry.Value);
                classes.Add(@class);
            }

            return string.Join(Environment.NewLine, classes);
        }

        private string GenerateAttributeArchetypeClass(string name, List<string> types)
        {
            return new AttributeArchetypeClassTemplate().Generate(name, GenerateAttributeArchetypeMethod(name, types));
        }

        private string GenerateAttributeArchetypeMethod(string attributeName, List<string> types)
        {
            var properTypes = types.Select(x => $"Attribute{x}").ToList();
            return new AttributeArchetypeMethodTemplate().Generate(attributeName, properTypes);
        }

    }
}



//------------------------------------------------------------------------------
// <auto-generated>
//     This code was auto-generated by Gameplay Ability System - Attribute Group Editor
//     version 1
//     from Assets/Gameplay Ability System DOTS/AttributeSystem/test.attributegroup
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

