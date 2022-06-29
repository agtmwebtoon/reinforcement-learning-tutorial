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
            float yRotation = UnityEngine.Random.Range(-5f, 5f);
        }
    
    }
}
