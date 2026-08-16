using System;
using System.Collections.Generic;
using UnityEngine;

namespace BeeKingdom.Buildings.Interaction
{
    public sealed class BuildingInteractionRegistry
    {
        private readonly Dictionary<GameObject, string> _byObject = new Dictionary<GameObject, string>();
        private readonly Dictionary<string, GameObject> _objectsByType = new Dictionary<string, GameObject>();
        private readonly Dictionary<string, GameObject> _objectsByLegacy = new Dictionary<string, GameObject>();

        public int Count
        {
            get { return _byObject.Count; }
        }

        public void Register(GameObject go, string buildingType)
        {
            if (go == null) throw new ArgumentNullException("go");
            if (string.IsNullOrEmpty(buildingType)) throw new ArgumentNullException("buildingType");

            string legacy = BuildingMappingTable.ToLegacyKey(buildingType);

            _byObject[go] = buildingType;
            _objectsByType[buildingType] = go;
            _objectsByLegacy[legacy] = go;
        }

        public bool Unregister(GameObject go)
        {
            if (go == null) return false;
            string buildingType;
            if (!_byObject.TryGetValue(go, out buildingType)) return false;
            _byObject.Remove(go);

            GameObject byType;
            if (_objectsByType.TryGetValue(buildingType, out byType) && byType == go)
                _objectsByType.Remove(buildingType);

            string legacy = BuildingMappingTable.ToLegacyKey(buildingType);
            GameObject byLegacy;
            if (_objectsByLegacy.TryGetValue(legacy, out byLegacy) && byLegacy == go)
                _objectsByLegacy.Remove(legacy);

            return true;
        }

        public BuildingDefinition GetBuilding(GameObject go)
        {
            BuildingDefinition definition;
            if (!TryGetBuilding(go, out definition))
                throw new KeyNotFoundException("Aucun bâtiment enregistré pour le GameObject " + go.name);
            return definition;
        }

        public bool TryGetBuilding(GameObject go, out BuildingDefinition definition)
        {
            definition = null;
            if (go == null) return false;
            string buildingType;
            if (!_byObject.TryGetValue(go, out buildingType)) return false;
            return BuildingCatalog.TryGetByBuildingType(buildingType, out definition);
        }

        public string GetBuildingType(GameObject go)
        {
            string buildingType;
            if (!_byObject.TryGetValue(go, out buildingType))
                throw new KeyNotFoundException("Aucun bâtiment enregistré pour le GameObject " + go.name);
            return buildingType;
        }

        public BuildingDefinition GetByBuildingType(string buildingType)
        {
            GameObject go;
            if (!_objectsByType.TryGetValue(buildingType, out go))
                throw new KeyNotFoundException("Aucun bâtiment enregistré pour buildingType " + buildingType);
            return GetBuilding(go);
        }

        public BuildingDefinition GetByLegacyKey(string legacyKey)
        {
            GameObject go;
            if (!_objectsByLegacy.TryGetValue(legacyKey, out go))
                throw new KeyNotFoundException("Aucun bâtiment enregistré pour legacyKey " + legacyKey);
            return GetBuilding(go);
        }

        public GameObject GetGameObjectByBuildingType(string buildingType)
        {
            GameObject go;
            if (!_objectsByType.TryGetValue(buildingType, out go))
                throw new KeyNotFoundException("Aucun GameObject enregistré pour buildingType " + buildingType);
            return go;
        }

        public GameObject GetGameObjectByLegacyKey(string legacyKey)
        {
            GameObject go;
            if (!_objectsByLegacy.TryGetValue(legacyKey, out go))
                throw new KeyNotFoundException("Aucun GameObject enregistré pour legacyKey " + legacyKey);
            return go;
        }

        public void Clear()
        {
            _byObject.Clear();
            _objectsByType.Clear();
            _objectsByLegacy.Clear();
        }
    }
}