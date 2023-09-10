using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreeFruit : MonoBehaviour
{

    [Tooltip("The vertices on the model to place fruit")]
    [SerializeField] public int[] m_modelVertices;

    [Tooltip("The vertices on the model to place fruit")]
    [SerializeField] public Vector3[] m_VertexPos;

    [Tooltip("The gizmo color for selected vertices")]
    [SerializeField] public Color m_gizmoColor = Color.red;

    [Tooltip("The gizmo size")]
    [SerializeField] public float m_gizmoSize = 0.1f;

    [Tooltip("Temp property for debugging")]
    [SerializeField] public bool m_StopClear;

}
