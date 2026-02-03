using UnityEngine;

/// <summary>
/// Componente auxiliar para guardar información del prefab original en objetos del pool
/// Esto permite que ReturnToPool sea más eficiente al no tener que buscar el prefab
/// </summary>
public class PooledObjectInfo : MonoBehaviour
{
    [HideInInspector]
    public GameObject originalPrefab;
}
