using UnityEngine;
using UnityEngine.UI;

public class WFCVisualizer : MonoBehaviour
{
    public WaveFunctionCollapse wfc;
    
    public Button stepButton;
    public Button autoButton;
    public Button resetButton;
    public Button visualizeButton;
    
    public Text statusText;
    
    void Start()
    {
        stepButton.onClick.AddListener(OnStepButtonClicked);
        autoButton.onClick.AddListener(OnAutoButtonClicked);
        resetButton.onClick.AddListener(OnResetButtonClicked);
        visualizeButton.onClick.AddListener(OnVisualizeButtonClicked);
        
        UpdateStatusText();
    }
    
    void OnStepButtonClicked()
    {
        wfc.StepCollapse();
        UpdateStatusText();
    }
    
    void OnAutoButtonClicked()
    {
        wfc.ToggleStepMode();
        UpdateStatusText();
    }
    
    void OnResetButtonClicked()
    {
        wfc.Reset();
        UpdateStatusText();
    }
    
    void OnVisualizeButtonClicked()
    {
        wfc.UpdateAllPossibilityVisuals();
    }
    
    void UpdateStatusText()
    {
        if (wfc.isStepMode)
        {
            statusText.text = "Step Mode - Step: " + wfc.currentStep;
        }
        else
        {
            statusText.text = "Auto Mode";
        }
    }
}