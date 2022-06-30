using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlowerArea : MonoBehaviour
{
    public const float AreaDiameter = 20f;

    private List<GameObject> flowerPlants;

    private Dictionary<Collider, Flower> nectarFlowerDictionary;

    public List<Flower> Flowers { get; private set; }
    
    public void ResetFlowers() 
    {
        foreach (GameObject flowerPlant in flowerPlants)
        {
            float xRotation = UnityEngine.Random.Range(-5f, 5f);
            float yRotation = UnityEngine.Random.Range(-180f, 180f);
            float zRotation = UnityEngine.Random.Range(-5f, 5f);

            flowerPlant.transform.localRotation = Quaternion.Euler(xRotation, yRotation, zRotation);
        }
        
        // Reset each flower
        foreach (Flower flower in Flowers)
        {
            flower.ResetFlower();
        }
    }

    public Flower GetFlowerFromNectar(Collider collider)
    {
        return nectarFlowerDictionary[collider];
    }

    private void Awake()
    {
        flowerPlants = new List<GameObject>();
        nectarFlowerDictionary = new Dictionary<Collider, Flower>();
        Flowers = new List<Flower>();
    }

    private void Start()
    {
        FindChildFlowers(transform);
    }
    
    /// <summary>
    /// Recursively finds all flowers and flower plants that are children of a parent transform
    /// </summary>
    /// <param name="parent">The parent of the children to check</param>
    private void FindChildFlowers(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.CompareTag("flower_plant"))
            {
                flowerPlants.Add(child.gameObject);
                FindChildFlowers(child);
            }
            else
            {
                Flower flower = child.GetComponent<Flower>();
                if (flower != null)
                {
                    
                    Flowers.Add(flower);
                    nectarFlowerDictionary.Add(flower.nectorCollider, flower);
                }
                else
                {
                    FindChildFlowers(child);
                }
            }
        }
    }
}
