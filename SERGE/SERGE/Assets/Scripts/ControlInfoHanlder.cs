using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ControlInfoHanlder : MonoBehaviour
{
    private Image background;
    private TMP_Text text;
    public GameObject cardcontrols;
    public GameObject faseTesto, faseSottotesto;
    private void Start()
    {
        background = gameObject.GetComponent<Image>();
        text = gameObject.GetComponentInChildren<TMP_Text>();

        background.enabled = false;
        text.enabled = false;

    }
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Z)) 
        {
            background.enabled = !background.enabled;
            text.enabled = !text.enabled;
        }
        if (background.enabled == false && text.enabled == false && GameMechanics.gameState.Equals(GameState.RiskAnalysis) && GameMechanics.isGameStarted)
        {
            cardcontrols.SetActive(true);
        }
        else if (background.enabled == true && text.enabled == true && GameMechanics.gameState.Equals(GameState.RiskAnalysis) && GameMechanics.isGameStarted)
        {
            cardcontrols.SetActive(false);
        }
        if (background.enabled == false && text.enabled == false && GameMechanics.isGameStarted)
        {
            faseTesto.GetComponent<TMP_Text>().alpha = 1.0f;
            faseSottotesto.GetComponent<TMP_Text>().alpha = 1.0f;
        }
        if(background.enabled == true && text.enabled == true && GameMechanics.isGameStarted)
        {
            faseTesto.GetComponent<TMP_Text>().alpha = 0f;
            faseSottotesto.GetComponent<TMP_Text>().alpha = 0f;
        }
    }
}
