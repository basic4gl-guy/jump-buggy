using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarDamage : MonoBehaviour
{
    public SkinnedMeshRenderer m_MeshDamage;

    [SerializeField] private float m_DamageForceMultiplier = 0.05f;
    [SerializeField] private float m_MinRelativeVelocity = 2.0f;
    [SerializeField] private float m_MaxDamage = 400.0f;
    [SerializeField] private int m_BlendShapeCount = 4;

    private Rigidbody m_Rigidbody;    
    private Material m_MaterialDamage;
    private float m_AccumulatedDamage;

    public bool IsTotalled() { return m_AccumulatedDamage >= m_MaxDamage;  }

    public void ResetDamage()
    {
        m_AccumulatedDamage = 0;
        if (m_MeshDamage != null)
        {
            for (int i = 0; i < m_BlendShapeCount; ++i)
            {
                m_MeshDamage.SetBlendShapeWeight(i, 0);
            }
        }

        if (m_MaterialDamage != null && m_MaterialDamage.HasProperty("_Mix"))
        {
            m_MaterialDamage.SetFloat("_Mix", 0.0f);
        }
    }

    // Use this for initialization
    void Start()
    {
        m_Rigidbody = GetComponent<Rigidbody>();

        if (m_MeshDamage != null)
        {
            Renderer meshRender = m_MeshDamage.GetComponent<Renderer>();
            m_MaterialDamage = meshRender.material;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        float curVelocity = Vector3.Magnitude(collision.relativeVelocity);
        if (curVelocity >= m_MinRelativeVelocity)
        {
            float curDamage = curVelocity * m_DamageForceMultiplier;
            m_AccumulatedDamage += curDamage;

            // show damage with mesh deformation
            if (m_MeshDamage != null)
            {
                for (int i = 0; i < m_BlendShapeCount; ++i)
                {
                    m_MeshDamage.SetBlendShapeWeight(i, Mathf.Clamp(m_AccumulatedDamage - (i * (m_MaxDamage / m_BlendShapeCount)), 0.0f, 100.0f));
                }
            }

            // show damage on texture 
            if (m_MaterialDamage != null && m_MaterialDamage.HasProperty("_Mix"))
            {
                m_MaterialDamage.SetFloat("_Mix", Mathf.Clamp(m_AccumulatedDamage / m_MaxDamage, 0.0f, 1.0f));
            }
        }
    }

    void Update()
    {
       
    }
}
