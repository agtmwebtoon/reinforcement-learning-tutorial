using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Change color when collision happen
/// </summary>
public class Flower : MonoBehaviour
{
    [Tooltip("The color when the flower is empty")]
    public Color emptyFlowerColor = new Color(.5f, 0f, 1f);
        
    [Tooltip("The color when the flower is fiiled")]
    public Color fillFlowerColor = new Color(1f, 0f, .2f);
        
    /// <summary>
    /// The trigger collider representing the nector
    /// </summary>
    [HideInInspector] public Collider nectorCollider;

    private Collider flowerCollider;

    private Material flowerMaterial;

    /// <summary>
    /// A vector pointing straight out of the flower
    /// </summary>
    /// <returns></returns>

    public Vector3 FlowerUpVector
    {
        get
        {
            return nectorCollider.transform.up;
        }
    }

    /// <summary>
    /// The center position of the nectar collider
    /// </summary>
    public Vector3 FlowerCenterPosition
    {
        get
        {
            return nectorCollider.transform.position;
        }
    }
    
    /// <summary>
    /// The amount of nector remaining in the flower
    /// </summary>
    public float NectorAmount { get; private set; }
    
    /// <summary>
    /// Whether the flower has any nectar remaining 
    /// </summary>
    public bool HasNector
    {
        get
        {
            return NectorAmount > 0f;
        }
    }

    /// <summary>
    /// Attempts to remove nectar from the flower
    /// </summary>
    /// <param name="amount">The amount of nectar to remove</param>
    /// <returns>The actual amout successfully removed</returns>
    public float Feed(float amount)
    {
        // Track how much nectar was successfully taken

        float nectarTaken = Mathf.Clamp(amount, 0f, NectorAmount);
        NectorAmount -= amount;

        if (NectorAmount <= 0)
        {
            // No nectar remaining
            NectorAmount = 0;
            nectorCollider.gameObject.SetActive(false);
            flowerCollider.gameObject.SetActive(false);

            //Change the flower color to indicate that it is empty
            flowerMaterial.SetColor("_BaseColor", emptyFlowerColor);
        }

        return nectarTaken;
    }
    
    /// <summary>
    /// Resets the flower
    /// </summary>
    public void ResetFlower()
    {
        // Refill the nectar
        NectorAmount = 1f;
        
        // Enable the flower and nectar colliders
        nectorCollider.gameObject.SetActive(true);
        flowerCollider.gameObject.SetActive(true);
        
        flowerMaterial.SetColor("_BaseColor", fillFlowerColor);

    }
    
    private void Awake()
    {
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        flowerMaterial = meshRenderer.material;

        flowerCollider = transform.Find("FlowerCollider").GetComponent<Collider>();
        nectorCollider = transform.Find("FlowerNectarCollider").GetComponent<Collider>();
    }
}
