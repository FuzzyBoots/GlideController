using FS_ThirdPerson;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace FS_Core
{
    public enum HitType { Any, Melee, Ranged };

    public class Damagable : MonoBehaviour, ISavable
    {
        AnimGraph animGraph;

        [field: SerializeField] public virtual float MaxHealth { get; set; } = 100;
        [SerializeField] HitType canBeDamagedBy;

        public virtual float CurrentHealth { get; set; }
        public event Action OnHealthUpdated;
        public event Action OnDead;
        public virtual Damagable Parent => this;
        public virtual bool CanTakeHit { get; set; } = true;

        ItemAttacher itemAttacher;
        private void Awake()
        {
            CurrentHealth = MaxHealth;
            animGraph = GetComponent<AnimGraph>();
            itemAttacher = GetComponent<ItemAttacher>();
        }

        public virtual void TakeDamage(float damage, HitType damageType)
        {
            if (canBeDamagedBy != HitType.Any && canBeDamagedBy != damageType) return;

            UpdateHealth(-damage);

            if (CurrentHealth <= 0)
            {
                OnDead?.Invoke();
            }
        }


        public virtual void UpdateHealth(float hpRestore)
        {
            CurrentHealth = Mathf.Clamp(CurrentHealth + hpRestore, 0, MaxHealth);
            OnHealthUpdated?.Invoke();
        }

        public void Dead(List<AnimGraphClipInfo> deathAnimations, Action OnDead)
        {
            StartCoroutine(PlayDeathAnimation(deathAnimations, OnDead));
        }

        IEnumerator PlayDeathAnimation(List<AnimGraphClipInfo> deathAnimations, Action OnDead)
        {
            if (deathAnimations.Count > 0 && animGraph != null)
                yield return animGraph.CrossfadeAsync(deathAnimations[UnityEngine.Random.Range(0, deathAnimations.Count)], transitionBack: false);
            OnDead?.Invoke();
        }

        public float GetDefense()
        {
            return itemAttacher != null? itemAttacher.GetTotalDefense() : 0;
        }

        public object CaptureState()
        {
            var saveData = new DamagableSaveData()
            {
                currentHealth = CurrentHealth
            };

            return saveData;
        }

        public void RestoreState(object state)
        {
            var saveData = state as DamagableSaveData;
            CurrentHealth = saveData.currentHealth;

            OnHealthUpdated?.Invoke();
        }

        public Type GetSavaDataType()
        {
            return typeof(DamagableSaveData);
        }
    }

    [Serializable]
    public class DamagableSaveData
    {
        public float currentHealth;
    }
}