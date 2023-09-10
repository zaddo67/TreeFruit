using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.IO;
using Forge.Networking.Unity.Messages.Interpreters;

[CustomEditor(typeof(TreeManager), true)]

public class TreeManagerEditor : Editor
{
    private Texture2D[] m_Textures;
    private Texture2D[] m_TexturesSelected;
    private Terrain m_cachedTerrain;
    private int m_treeIndex;
    private int[] m_modelVertices;

    Color m_saveColor= Color.white;

    public override void OnInspectorGUI()
    {

        m_saveColor = EditorStyles.label.normal.textColor;

        TreeManager p = (TreeManager)target;
        if (p == null) return;
        m_treeIndex = p.m_treeIndex;
        UpdateTerrain(p);

        serializedObject.Update();
        EditorGUI.BeginChangeCheck();

        base.DrawDefaultInspector();

        if (m_Textures != null)
        {
            var rowCount = (Screen.width - 50) / 100;
            for (int i = 0; i < m_Textures.Length; i++)
            {
                if (i % rowCount == 0)
                {
                    if (i > 0) EditorGUILayout.EndHorizontal();
                    EditorGUILayout.BeginHorizontal();
                }
                TreeButton(i);
            }
            EditorGUILayout.EndHorizontal();
        }

        if (p.m_treeIndex!= m_treeIndex)
        {
            p.m_treeIndex= m_treeIndex;
            GUI.changed = true;
            EditorUtility.SetDirty(this);
        }

        if (GUILayout.Button("Refresh Trees", GUILayout.Width(150), GUILayout.Height(20)))
        {
            GUI.changed = true;
            EditorUtility.SetDirty(target);
        }

        if (GUI.changed)
        {
            LoadTrees(p);
        }
        //Label Color
        EditorStyles.label.normal.textColor = Color.yellow;

        EditorGUILayout.BeginVertical();
        if (p.m_instances!= null) 
            EditorGUILayout.LabelField($"Number of trees found: {p.m_instances.Length}", GUILayout.MaxWidth(800));
        else
            EditorGUILayout.LabelField($"No trees found", GUILayout.MaxWidth(800));
        EditorGUILayout.EndVertical();

        EditorStyles.label.normal.textColor = m_saveColor;

        // If any control changed, then apply changes
        if (EditorGUI.EndChangeCheck())
        {
            UpdateTerrain(p);
            serializedObject.ApplyModifiedProperties();
        }
    }
    

    private void UpdateTerrain(TreeManager p)
    {
        if (m_cachedTerrain != p.m_terrain)
        {
            m_cachedTerrain= p.m_terrain;
            var prototypes = m_cachedTerrain.terrainData.treePrototypes;
            m_Textures = new Texture2D[prototypes.Length];
            m_TexturesSelected = new Texture2D[prototypes.Length];
            for (int i=0; i<prototypes.Length;i++)
            {
                var prefab = prototypes[i].prefab;
                if (prefab != null)
                {
                    Texture2D pr = AssetPreview.GetAssetPreview(prefab);
                    if (pr != null)
                    {
                        m_Textures[i] = pr;
                        m_TexturesSelected[i] = AddBorder(pr, Color.green, 2);
                    }
                }
            }
            GUI.changed = true;
        }
    }

    private void LoadTrees(TreeManager p)
    {
        List<int> instances = new List<int>();
        List<Vector3> positions = new List<Vector3>();
        List<Vector3> rotations = new List<Vector3>();
        List<Vector3> scales = new List<Vector3>();

        for (int i=0; i<p.m_terrain.terrainData.treeInstanceCount; i++)
        {
            var treeInstance = p.m_terrain.terrainData.GetTreeInstance(i);
            if (treeInstance.prototypeIndex == p.m_treeIndex)
            {
                instances.Add(i);
                Vector3 position = new Vector3();
                position.x = treeInstance.position.x * p.m_terrain.terrainData.size.x;
                position.y = treeInstance.position.y * p.m_terrain.terrainData.size.y;
                position.z = treeInstance.position.z * p.m_terrain.terrainData.size.z;
                positions.Add(position);
                rotations.Add(new Vector3(0f, treeInstance.rotation * Mathf.Rad2Deg, 0f));
                scales.Add(new Vector3(treeInstance.widthScale, treeInstance.heightScale, treeInstance.widthScale));

            }
        }
        p.m_instances= instances.ToArray();
        p.m_positions= positions.ToArray();
        p.m_rotations= rotations.ToArray();
        p.m_scales= scales.ToArray();
    }


    private Texture2D AddBorder(Texture2D texture, Color color, int width)
    {
        Texture2D copyTexture = new Texture2D(texture.width, texture.height);
        copyTexture.SetPixels(texture.GetPixels());

        int w = texture.width;
        int h = texture.height;

        for (int i=0; i<width; i++)
        {
            for (int y=0; y<h; y++)
            {
                copyTexture.SetPixel(i, y, color);
                copyTexture.SetPixel(w-i,y,color);
            }
        }

        for (int i=0; i<width;i++)
        {
            for (int x = 0; x < w; x++)
            {
                copyTexture.SetPixel(x, i, color);
                copyTexture.SetPixel(x, h-i, color);
            }

        }
        copyTexture.Apply();
        return copyTexture;
    }

    private void TreeButton(int i)
    {
        GUIContent button_tex;
        if (i==m_treeIndex)
            button_tex = new GUIContent(m_TexturesSelected[i]);
        else
            button_tex = new GUIContent(m_Textures[i]);

        if (GUILayout.Button(button_tex, GUILayout.Width(80), GUILayout.Height(80)))
        {
            m_treeIndex= i;
        }
    }

}
