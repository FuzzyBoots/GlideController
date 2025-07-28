using FS_Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace FS_ThirdPerson
{

    public class HPBar : MonoBehaviour
    {
        [SerializeField] Damagable damagable;
        [SerializeField] Image healthBarImg;
        [SerializeField] TMP_Text healthTxt;
        [SerializeField] bool lookAtCamera = true;


        float currentFillAmount = 1;

        Camera cam;


        private void Awake()
        {
            if (damagable == null)
                damagable = GetComponentInParent<Damagable>();
        }

        private void Start()
        {
            damagable.OnHealthUpdated += ControlHealthBar;
            cam = Camera.main;

            healthBarImg.fillAmount = damagable.CurrentHealth / damagable.MaxHealth;

            if (healthTxt != null)
                healthTxt.text = $"{Mathf.FloorToInt(damagable.CurrentHealth)}";
        }

        private void OnDestroy()
        {
            damagable.OnHealthUpdated -= ControlHealthBar;
        }

        void ControlHealthBar()
        {
            StartCoroutine(LerpHealth());
        }

        IEnumerator LerpHealth()
        {
            var fillAmount = damagable.CurrentHealth / damagable.MaxHealth;
            while (currentFillAmount > fillAmount)
            {
                fillAmount = damagable.CurrentHealth / damagable.MaxHealth;
                currentFillAmount = Mathf.MoveTowards(currentFillAmount, fillAmount, Time.deltaTime);
                healthBarImg.fillAmount = currentFillAmount;
                yield return null;
            }
            currentFillAmount = fillAmount;
            healthBarImg.fillAmount = currentFillAmount;
            if (healthTxt != null)
                healthTxt.text = $"{Mathf.FloorToInt(damagable.CurrentHealth)}";
            if (damagable.CurrentHealth <= 0)
            {
                yield return new WaitForSecondsRealtime(1f);
                this.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            if (lookAtCamera)
                transform.rotation = Quaternion.LookRotation(cam.transform.forward);
        }
    }
}