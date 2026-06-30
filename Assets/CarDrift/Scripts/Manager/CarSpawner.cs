using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class CarSpawner : MonoBehaviour
{
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private bool registerAsPlayer = true;
    [SerializeField] private bool engineRunningOnSpawn = true;
    [SerializeField] private List<CarCatalogEntry> cars = CarCatalog.CreateDefaultEntries();

    public RCCP_CarController CurrentCar { get; private set; }
    public IReadOnlyList<CarCatalogEntry> Cars => cars;

    public RCCP_CarController SpawnSelectedCar()
    {
        return Spawn(DataManager.Instance.SelectedCar, true);
    }

    public RCCP_CarController Spawn(CarType carType, bool applySavedUpgrade)
    {
        CarCatalogEntry entry = GetEntry(carType);
        if (entry == null || !entry.prefab)
        {
            Debug.LogError($"Car prefab is missing for {carType}. Add it to {nameof(CarSpawner)} cars.");
            return null;
        }

        if (CurrentCar)
            Destroy(CurrentCar.gameObject);

        Vector3 position = spawnPoint ? spawnPoint.position : transform.position;
        Quaternion rotation = spawnPoint ? spawnPoint.rotation : transform.rotation;

        CurrentCar = RCCP.SpawnRCC(entry.prefab, position, rotation, registerAsPlayer, true, engineRunningOnSpawn);

        if (applySavedUpgrade)
            CarUpgradeApplier.Apply(CurrentCar, DataManager.Instance.GetOrCreateUpgrade(carType));

        return CurrentCar;
    }

    public CarCatalogEntry GetEntry(CarType carType)
    {
        return cars.Find(x => x.carType == carType);
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (cars == null || cars.Count == 0)
            cars = CarCatalog.CreateDefaultEntries();

        foreach (CarType carType in CarCatalog.AllCars)
        {
            CarCatalogEntry entry = cars.Find(x => x.carType == carType);
            if (entry == null)
            {
                entry = new CarCatalogEntry { carType = carType, price = cars.Count == 0 ? 0 : 1500 + cars.Count * 500 };
                cars.Add(entry);
            }

            entry.displayName = CarCatalog.GetDisplayName(carType);

            if (!entry.prefab)
                entry.prefab = AssetDatabase.LoadAssetAtPath<RCCP_CarController>(CarCatalog.GetPrefabPath(carType));
        }
    }
#endif
}
