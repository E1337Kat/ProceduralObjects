﻿using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;

using ProceduralObjects.ProceduralText;

namespace ProceduralObjects.Classes
{
    public class ClipboardProceduralObjects
    {
        public ClipboardProceduralObjects(ClipboardType type)
        {
            this.type = type;
        }

        public void MakeSelectionList(List<ProceduralObject> list, POGroup selectedGroup)
        {
            this.selection_objects = new Dictionary<CacheProceduralObject, Vector3>();
            for (int i = 0; i < list.Count; i++)
            {
                Vector3 relativePos;
                if (i == 0)
                    relativePos = Vector3.zero;
                else
                    relativePos = list[i].m_position - list[0].m_position;
                this.selection_objects.Add(new CacheProceduralObject(list[i]), relativePos);
            }
            if (selectedGroup != null)
            {
                groupInformation = null;
                return;
            }
            else
            {
                groupInformation = new Dictionary<CacheProceduralObject, CacheProceduralObject>();
                for (int i = 0; i < list.Count; i++)
                {
                    var obj = list[i];
                    if (obj.group != null)
                    {
                        if (obj.isRootOfGroup)
                            continue;
                        if (!list.Contains(obj.group.root))
                            continue;
                        groupInformation.Add(selection_objects.Keys.ToList()[i], 
                            selection_objects.Keys.ToList()[list.IndexOf(obj.group.root)]);
                    }
                }
            }
        }

        public CacheProceduralObject single_object;
        public Dictionary<CacheProceduralObject, Vector3> selection_objects;
        public ClipboardType type;
        public Dictionary<CacheProceduralObject, CacheProceduralObject> groupInformation;

        public void RecreateGroups(Dictionary<CacheProceduralObject, ProceduralObject> createdObjects)
        {
            if (groupInformation == null)
                return;
            if (groupInformation.Count == 0)
                return;

            foreach (var kvp in createdObjects)
            {
                if (!groupInformation.ContainsKey(kvp.Key))
                    continue;

                if (createdObjects[groupInformation[kvp.Key]].group == null)
                {
                    var group = POGroup.CreateGroupWithRoot(createdObjects[groupInformation[kvp.Key]]);
                    ProceduralObjectsLogic.instance.groups.Add(group);
                }

                createdObjects[groupInformation[kvp.Key]].group.AddToGroup(kvp.Value);
            }
        }

        public void ExportSelection(string name, ExternalProceduralObjectsManager manager, bool staticImport)
        {
            if (selection_objects == null)
                return;
            if (selection_objects.Count <= 1)
                return;
            string path = ProceduralObjectsMod.ExternalsConfigPath + name.ToFileName() + ".pobj";
            if (File.Exists(path))
                return;

            TextWriter tw = new StreamWriter(path);
            tw.WriteLine("externaltype = " + (staticImport ? "static" : "selection"));
            tw.WriteLine("name = " + name);
            foreach (KeyValuePair<CacheProceduralObject, Vector3> kvp in selection_objects)
            {
                tw.WriteLine("OBJECT");
                tw.WriteLine("{");
                tw.WriteLine("baseInfoType = " + kvp.Key.baseInfoType);
                tw.WriteLine("basePrefabName = " + kvp.Key.basePrefabName);
                if (staticImport)
                    tw.WriteLine("absPosition = " + kvp.Key._staticPos.ToStringUnrounded());
                else
                    tw.WriteLine("relativePosition = " + kvp.Value.ToStringUnrounded());
                tw.WriteLine("isPloppableAsphalt = " + kvp.Key.isPloppableAsphalt.ToString());
                //  tw.WriteLine("scale = " + pobj.scale.ToString());
                tw.WriteLine("customTexture = " + ((kvp.Key.customTexture == null) ? "null" : kvp.Key.customTexture.name));
                tw.WriteLine("renderDistance = " + kvp.Key.renderDistance.ToString());
                tw.WriteLine("renderDistLocked = " + kvp.Key.renderDistLocked.ToString());
                tw.WriteLine("rotation = " + kvp.Key.m_rotation.ToStringUnrounded());
                tw.WriteLine("disableRecalculation = " + kvp.Key.disableRecalculation.ToString());
                if (kvp.Key.tilingFactor != 8)
                    tw.WriteLine("tilingFactor = " + kvp.Key.tilingFactor.ToString());
                tw.WriteLine("color = " + ((SerializableColor)kvp.Key.m_color).ToString());
                tw.WriteLine("flipFaces = " + kvp.Key.flipFaces.ToString());
                tw.WriteLine("normalsRecalc = " + kvp.Key.normalsRecalculation.ToString());
                tw.WriteLine("visibility = " + kvp.Key.visibility.ToString());
                if (kvp.Key.textParam != null)
                {
                    if (kvp.Key.textParam.Count() > 0)
                    {
                        foreach (TextField field in kvp.Key.textParam.m_textFields)
                        {
                            tw.WriteLine(TextField.SaveString(field));
                        }
                    }
                }
                if (kvp.Key.modules != null)
                {
                    if (kvp.Key.modules.Count > 0)
                        ModuleManager.WriteModules(tw, kvp.Key.modules, false);
                }
                if (kvp.Key.meshStatus == 1)
                    tw.WriteLine("ORIGINALMODEL");
                else
                {
                    tw.WriteLine("VERTICES " + kvp.Key.allVertices.Count());
                    for (int i = 0; i < kvp.Key.allVertices.Count(); i++)
                    {
                        tw.WriteLine("vertex " + i.ToString() + " = " + kvp.Key.allVertices[i].ToStringUnrounded());
                    }
                }
                tw.WriteLine("}");
            }
            tw.Close();
        }

        public enum ClipboardType
        {
            Single,
            Selection
        }
    }
}
