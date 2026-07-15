using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DismembermentUI : MonoBehaviour
{
    private GameObject dismembermentCanvas;
    private List<Button> buttons = new List<Button>();
    [SerializeField] private MeshExtractor meshExtractor;
    int offset = 30;
    [SerializeField] int initialPosition = 350;
    int tempPosition = 0;
    private void Awake()
    {
        dismembermentCanvas = gameObject;
    }

    private void Start()
    {
        buttons.AddRange(dismembermentCanvas.GetComponentsInChildren<Button>());

        foreach (var button in buttons)
        {
            tempPosition = initialPosition - offset;
            button.transform.localPosition = new Vector3(-700, tempPosition-50, 0);
            string name = button.name;
            button.onClick.AddListener(() => meshExtractor.DismemberBone("mixamorig:" + name));
            var buttonText = button.GetComponentInChildren<TMP_Text>();
            buttonText.text = name;
            initialPosition = tempPosition;
        }
    }
}
