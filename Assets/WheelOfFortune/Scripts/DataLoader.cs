using UnityEngine;
using UnityEngine.UI;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Random = UnityEngine.Random;
using TMPro;

public class DataLoader : MonoBehaviour
{
    List<RewardItem> rewardList;
    List<RewardItem> shuffledList;
    public WheelDivision[] wheelDivisions;
    public GameObject spinningWheel;
    public float wheelSpinTime;
    private float elapsedTime = 0f;
    private bool isRotating = false;
    private float targetAngle;
    private float initialOffset = 45f;

    //Rewards
    int coinsAmount = 0;
    int multiplierAmount = 0;
    int rewardAmount = 0;

    //UI
    public Button spinButton;
    public TextMeshProUGUI multiplierText;
    public TextMeshProUGUI rewardText;
    public GameObject coinLogo;

    //Data
    TextAsset dataFile;


    [System.Serializable]
    public class RewardData
    {
        public int coins;
        public List<RewardItem> rewards;
    }

    [System.Serializable]
    public class RewardItem
    {
        public int multiplier;
        public float probability;
        public string color;
    }

    public RewardData jsonData;

    private void Start()
    {
        Init();
    }

    public void Init()
    {
        multiplierText.text = "";
        rewardText.text = "";
        coinLogo.SetActive(false);
        dataFile = Resources.Load<TextAsset>("data");

        if (dataFile != null)
        {
            string json = dataFile.text;
            jsonData = JsonConvert.DeserializeObject<RewardData>(json);

            coinsAmount = jsonData.coins;
            rewardList = jsonData.rewards;
            PopulateWheelData();
        }
        else
        {
            Debug.LogError("JSON file not found");
        }
    }

    public void PopulateWheelData()
    {
        List<RewardItem> shuffledList = ShuffleList(rewardList);
        for(int i = 0; i < shuffledList.Count; i++)
        {
            wheelDivisions[i].multiplier.text = "x" + shuffledList[i].multiplier.ToString();
            if (ColorUtility.TryParseHtmlString(shuffledList[i].color, out Color color))
            {
                wheelDivisions[i].divisionImage.color = color;
            }
            wheelDivisions[i].probability = shuffledList[i].probability;
        }
    }

    private List<RewardItem> ShuffleList<RewardItem>(List<RewardItem> list)
    {
        List<RewardItem> shuffledList = new List<RewardItem>(list);

        int n = shuffledList.Count;
        while (n > 1)
        {
            n--;
            int k = Random.Range(0, n + 1);
            RewardItem value = shuffledList[k];
            shuffledList[k] = shuffledList[n];
            shuffledList[n] = value;
        }

        return shuffledList;
    }

    public void Spin()
    {
        spinButton.interactable = false;
        float randomProbability = Random.Range(0.01f, 1);
        float calculatedProbability = 0;
        foreach (RewardItem reward in rewardList)
        {
            randomProbability -= reward.probability;
            if (randomProbability <= 0)
            {
                Debug.Log("Calculated Prob " + reward.probability);
                calculatedProbability = reward.probability;
                multiplierAmount = reward.multiplier;
                break;
            }
        }
        foreach(WheelDivision div in wheelDivisions)
        {
            if(div.probability == calculatedProbability)
            {
                int randomRotationCycles = Random.Range(2, 4);
                targetAngle = (randomRotationCycles * 360) + 360 - div.gameObject.transform.localEulerAngles.z - initialOffset;
                if (!isRotating)
                {
                    StartCoroutine(SpinToTargetAngle());
                }
                break;
            }
        }
    }

    private IEnumerator SpinToTargetAngle()
    {
        isRotating = true;
        float initialRotation = transform.eulerAngles.z;

        while (elapsedTime < wheelSpinTime)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / wheelSpinTime);
            t = 1 - Mathf.Pow(1 - t, 2);
            float currentAngle = Mathf.Lerp(initialRotation, targetAngle, t);

            spinningWheel.transform.eulerAngles = new Vector3(0, 0, currentAngle);

            yield return null;
        }

        spinningWheel.transform.eulerAngles = new Vector3(0, 0, targetAngle);

        isRotating = false;
        elapsedTime = 0f;
        StartCoroutine(ShowRewards());
    }

    private IEnumerator ShowRewards()
    {
        var calculatedReward = coinsAmount * multiplierAmount;
        multiplierText.text = "x" + multiplierAmount.ToString();
        yield return new WaitForSeconds(1f);
        coinLogo.SetActive(true);
        rewardText.text = calculatedReward.ToString();
        yield return new WaitForSeconds(4f);
        multiplierText.text = "";
        rewardText.text = "";
        coinLogo.SetActive(false);
        spinButton.interactable = true;
        PopulateWheelData();
    }
}
