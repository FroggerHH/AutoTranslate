using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RavSoft.GoogleTranslator;
using TMPro;
using static TMPro.TMP_Dropdown;
using static ColorType;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

public class GameManager : MonoBehaviour
{
    const string ResultLanguageYML = "ResultLanguageYML";
    const string OriginalLanguageYML = "OriginalLanguageYML";
    const string ResultLanguageCode = "ResultLanguageCode";
    const string OriginalLanguageCode = "OriginalLanguageCode";
    const string OrigPath = "OrigPath";
    const string ResultPath = "ResultPath";
    const string SavedMode = "SavedMode";
    const string NameCode = "NameCode";
    const string DescriptionCode = "DescriptionCode";


    private Translator translator = new Translator();
    private static GameManager instance = null;

    [Header("YML mode")] [Space] [Header("Original")] [SerializeField]
    private GameObject YML_modePanel;

    [SerializeField] private TMP_InputField originalPathText;

    [SerializeField] private TMP_Dropdown originalLanguageDropdown_YML_mode;

    [Header("Result")] [SerializeField] private TMP_InputField resultPathText;
    [SerializeField] private TMP_Dropdown resultLanguageDropdown_YML_mode;

    [Space(10)] [Header("Code mode")] [Space] [SerializeField]
    private GameObject code_modePanel;

    [SerializeField] private TMP_InputField originalName;
    [SerializeField] private TMP_InputField originalDescription;
    [SerializeField] private TMP_Dropdown originalLanguageDropdown_code_mode;
    [SerializeField] private TMP_Dropdown resultLanguageDropdown_code_mode;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private Button copyButton;


    [Space(10)] [Header("Other")] [SerializeField]
    private Button YML_modeButton;

    [SerializeField] private GameObject translateButtonsGroup;
    [SerializeField] private Vector3 translateGroupPos_YML_mode;
    [SerializeField] private Vector3 translateGroupPos_code_mode;

    [SerializeField] private Button code_modeButton;
    [SerializeField] private Button translateButton;

    [Space(10)] [SerializeField] private Button stopTranslationButton;

    [SerializeField] private List<OptionData> optionDatas = new List<OptionData>()
    {
        new("-ALL-"),
        new("English"),
        new("Swedish"),
        new("French"),
        new("Italian"),
        new("German"),
        new("Spanish"),
        new("Russian"),
        new("Romanian"),
        new("Bulgarian"),
        new("Macedonian"),
        new("Finnish"),
        new("Danish"),
        new("Norwegian"),
        new("Icelandic"),
        new("Turkish"),
        new("Lithuanian"),
        new("Czech"),
        new("Hungarian"),
        new("Slovak"),
        new("Polish"),
        new("Dutch"),
        new("Portuguese"),
        new("Chinese"),
        new("Japanese"),
        new("Korean"),
        new("Hindi"),
        new("Thai"),
        new("Abenaki"),
        new("Croatian"),
        new("Georgian"),
        new("Greek"),
        new("Serbian"),
        new("Ukrainian")
    };

    [Space(10)] [Header("Debug")] [SerializeField]
    private TMP_Text debugText;


    [ContextMenu(nameof(SetTranslateGroupPos_YML))]
    private void SetTranslateGroupPos_YML() =>
        translateGroupPos_YML_mode = translateButtonsGroup.transform.localPosition;

    [ContextMenu(nameof(SetTranslateGroupPos_Code))]
    private void SetTranslateGroupPos_Code() =>
        translateGroupPos_code_mode = translateButtonsGroup.transform.localPosition;

    private string resultPath => resultPathText.text;
    private string origPath => originalPathText.text;
    private string origPathFile => origPath + $"\\{origLanguage_YML}.yml";

    private string origLanguage_YML =>
        originalLanguageDropdown_YML_mode.options[originalLanguageDropdown_YML_mode.value].text;

    private string resultLanguage_YML =>
        resultLanguageDropdown_YML_mode.options[resultLanguageDropdown_YML_mode.value].text;

    private string origLanguage_Code =>
        originalLanguageDropdown_code_mode.options[originalLanguageDropdown_code_mode.value].text;

    private string resultLanguage_Code =>
        resultLanguageDropdown_code_mode.options[resultLanguageDropdown_code_mode.value].text;

    private Mode currentMode => (Mode)(Enum.Parse(typeof(Mode), PlayerPrefs.GetString(SavedMode)));

    private void Awake()
    {
        instance = this;
        debugText.text = string.Empty;
        resultText.text = string.Empty;
        stopTranslationButton.interactable = false;
        originalName.onEndEdit.AddListener(_ =>
            PlayerPrefs.SetString(NameCode, originalName.text));
        originalDescription.onEndEdit.AddListener(_ =>
            PlayerPrefs.SetString(DescriptionCode, originalDescription.text));
        originalPathText.onEndEdit.AddListener(_ => CheckFile(origPathFile));
        resultPathText.onEndEdit.AddListener(_ => CheckDirectory(resultPath));
        translateButton.onClick.AddListener(TryTranslate);
        stopTranslationButton.onClick.AddListener((() =>
        {
            StopAllCoroutines();
            DoDebug("Stoped by user", yellow, 4);
        }));
        copyButton.onClick.AddListener((() =>
        {
            resultText.text.CopyToClipboard();
            DoDebug("Translation copied to clipboard", green, 4);
        }));

        resultLanguageDropdown_YML_mode.AddOptions(optionDatas);
        resultLanguageDropdown_YML_mode.onValueChanged.AddListener((_ => { CheckDirectory(resultPath); }));
        resultLanguageDropdown_YML_mode.onValueChanged.AddListener((_ =>
        {
            PlayerPrefs.SetString(ResultLanguageYML, resultLanguage_YML);
            if (origLanguage_YML == resultLanguage_YML)
                DoDebug($"Original language equals to result language: {resultLanguage_YML}", yellow, 3);
        }));
        originalLanguageDropdown_YML_mode.AddOptions(optionDatas.Where(x => x.text != "-ALL-").ToList());
        originalLanguageDropdown_YML_mode.onValueChanged.AddListener((_ => { CheckFile(origPathFile); }));
        originalLanguageDropdown_YML_mode.onValueChanged.AddListener((_ =>
        {
            PlayerPrefs.SetString(OriginalLanguageYML, origLanguage_YML);
            if (origLanguage_YML == resultLanguage_YML)
                DoDebug($"Original language equals to result language: {resultLanguage_YML}", yellow, 3);
        }));
        resultLanguageDropdown_code_mode.AddOptions(optionDatas);
        resultLanguageDropdown_code_mode.onValueChanged.AddListener((_ =>
        {
            PlayerPrefs.SetString(ResultLanguageCode, resultLanguage_Code);
            if (origLanguage_Code == resultLanguage_Code)
                DoDebug($"Original language equals to result language: {resultLanguage_Code}", yellow, 3);
        }));
        originalLanguageDropdown_code_mode.AddOptions(optionDatas.Where(x => x.text != "-ALL-").ToList());
        originalLanguageDropdown_code_mode.onValueChanged.AddListener((_ =>
        {
            PlayerPrefs.SetString(OriginalLanguageCode, origLanguage_Code);
            if (origLanguage_Code == resultLanguage_Code)
                DoDebug($"Original language equals to result language: {resultLanguage_Code}", yellow, 3);
        }));

        if (PlayerPrefs.HasKey(OrigPath)) originalPathText.text = PlayerPrefs.GetString(OrigPath);
        if (PlayerPrefs.HasKey(ResultPath)) resultPathText.text = PlayerPrefs.GetString(ResultPath);
        if (PlayerPrefs.HasKey(OriginalLanguageYML))
        {
            originalLanguageDropdown_YML_mode.value = originalLanguageDropdown_YML_mode.options.IndexOf(
                originalLanguageDropdown_YML_mode.options.Find(
                    x => x.text == PlayerPrefs.GetString(OriginalLanguageYML)));
        }

        if (PlayerPrefs.HasKey(ResultLanguageYML))
        {
            resultLanguageDropdown_YML_mode.value = resultLanguageDropdown_YML_mode.options.IndexOf(
                resultLanguageDropdown_YML_mode.options.Find(x => x.text == PlayerPrefs.GetString(ResultLanguageYML)));
        }

        if (PlayerPrefs.HasKey(OriginalLanguageCode))
        {
            originalLanguageDropdown_code_mode.value = originalLanguageDropdown_code_mode.options.IndexOf(
                originalLanguageDropdown_code_mode.options.Find(x =>
                    x.text == PlayerPrefs.GetString(OriginalLanguageCode)));
        }

        if (PlayerPrefs.HasKey(ResultLanguageCode))
        {
            resultLanguageDropdown_code_mode.value = resultLanguageDropdown_code_mode.options.IndexOf(
                resultLanguageDropdown_code_mode.options.Find(x =>
                    x.text == PlayerPrefs.GetString(ResultLanguageCode)));
        }

        if (PlayerPrefs.HasKey(NameCode))
        {
            originalName.text = PlayerPrefs.GetString(NameCode);
        }

        if (PlayerPrefs.HasKey(DescriptionCode))
        {
            originalDescription.text = PlayerPrefs.GetString(DescriptionCode);
        }

        YML_modeButton.onClick.AddListener((() =>
        {
            YML_modeButton.interactable = false;
            code_modeButton.interactable = true;
            PlayerPrefs.SetString(SavedMode, Mode.YML.ToString());
            translateButtonsGroup.transform.localPosition = translateGroupPos_YML_mode;

            YML_modePanel.SetActive(true);
            code_modePanel.SetActive(false);
        }));
        code_modeButton.onClick.AddListener((() =>
        {
            YML_modeButton.interactable = true;
            code_modeButton.interactable = false;
            PlayerPrefs.SetString(SavedMode, Mode.Code.ToString());
            translateButtonsGroup.transform.localPosition = translateGroupPos_code_mode;

            YML_modePanel.SetActive(false);
            code_modePanel.SetActive(true);
        }));

        if (PlayerPrefs.HasKey(SavedMode))
        {
            switch (currentMode)
            {
                case Mode.YML:
                    YML_modeButton.onClick?.Invoke();
                    break;
                case Mode.Code:
                    code_modeButton.onClick?.Invoke();
                    break;
                default:
                    DoDebug("Unknown saved mode: " + currentMode, ColorType.yellow);
                    break;
            }
        }
        else YML_modeButton.onClick?.Invoke();


        CheckFile(origPathFile);
        CheckDirectory(resultPath);
        translateButtonsGroup.SetActive(true);
        copyButton.interactable = false;
    }

    public void TryTranslate()
    {
        switch (currentMode)
        {
            case Mode.YML:
                if (!CheckFile(origPathFile, false)) return;
                if (!CheckDirectory(resultPath, false)) return;
                if (origLanguage_YML == resultLanguage_YML)
                {
                    DoDebug($"Original language equals to result language: {resultLanguage_YML}", yellow, 3);
                    return;
                }

                translateButton.interactable = false;
                if (resultLanguage_YML == "-ALL-")
                {
                    StartCoroutine(TranslateAll());
                }
                else
                {
                    StartCoroutine(Translate(resultLanguage_YML));
                }

                break;
            case Mode.Code:
                var str = string.Empty;
                if (resultLanguage_Code == "-ALL-")
                {
                    str += $"YOUR_ITEMNAME.Name";
                    foreach (var option in originalLanguageDropdown_code_mode.options)
                    {
                        str += AddLoc(originalName.text, option.text);
                    }

                    str += $";";

                    str += $"\n";

                    str += $"YOUR_ITEMNAME.Description";
                    foreach (var option in originalLanguageDropdown_code_mode.options)
                    {
                        str += AddLoc(originalDescription.text, option.text);
                    }

                    str += $";";
                }
                else
                {
                    str += $"YOUR_ITEMNAME.Name";
                    str += AddLoc(originalName.text, resultLanguage_Code);
                    str += $";";
                    str += $"\n";
                    str += $"YOUR_ITEMNAME.Description";
                    str += AddLoc(originalDescription.text, resultLanguage_Code);
                    str += $";";
                }

                resultText.text = str;
                copyButton.interactable = true;
                break;
        }

        string AddLoc(string text, string resultLanguage)
        {
            string str = string.Empty;
            var translate = translator.Translate(text, origLanguage_Code, resultLanguage);
            str += $"\n";
            str += $"    .{resultLanguage}(\"{translate}\")";
            return str;
        }
    }

    private IEnumerator TranslateAll()
    {
        foreach (var data in optionDatas.Where(x => x.text != "-ALL-" && x.text != origLanguage_YML))
        {
            var language = data.text;
            yield return StartCoroutine(Translate(language));
        }
    }

    private IEnumerator Translate(string resultLanguage)
    {
        translateButton.interactable = false;
        stopTranslationButton.interactable = true;
        DoDebug($"Translating to {resultLanguage}...", grey, 1);
        yield return new WaitForSeconds(0.1f);

        try
        {
            Dictionary<string, string> localized = new();

            foreach (var pair in LoadFromFileText(File.ReadAllText(origPathFile)))
            {
                localized.Add(pair.Key, Localize(pair.Value, origLanguage_YML, resultLanguage));
            }

            using (StreamWriter sw = new(resultPath + $"//{resultLanguage}.yml"))
            {
                sw.Write(SaveToStr(localized));
            }

            DoDebug($"Translation to {resultLanguage} completed", green);
        }
        catch (Exception e)
        {
            DoDebug(e.Message, red, 10);
        }

        if (translator.Error != null) DoDebug(translator.Error.Message, red, 3);

        translateButton.interactable = true;
        stopTranslationButton.interactable = false;
    }

    private bool CheckFile(string path, bool debug = true)
    {
        var exists = File.Exists(path);
        if (!exists)
        {
            if (debug) DoDebug($"File does not exist {path}", red, 3);
            return false;
        }
        else
        {
            if (debug) DoDebug($"Found file {path}", white, 3);
            PlayerPrefs.SetString("OrigPath", origPath);
            return true;
        }
    }

    private bool CheckDirectory(string path, bool debug = true)
    {
        var exists = Directory.Exists(path);
        if (!exists)
        {
            if (debug) DoDebug($"Directory does not exist {path}", red, 3);
            return false;
        }
        else
        {
            if (debug) DoDebug($"Found directory {path}", white, 3);
            PlayerPrefs.SetString("ResultPath", resultPath);
            return true;
        }
    }

    public void DoDebug(string msg, ColorType color, float clearDelay = 10)
    {
        StartCoroutine(DebugCoroutine(msg, color, clearDelay));
    }

    private IEnumerator DebugCoroutine(string msg, ColorType color, float clearDelay)
    {
        var text = $"\n<color={color}>{msg}</color>";
        debugText.text += text;
        yield return new WaitForSecondsRealtime(clearDelay);
        debugText.text = debugText.text.Replace(text, "");
    }

    public string Localize(string value, string origLanguage, string resultLanguage)
    {
        return translator.Translate(value, origLanguage, resultLanguage);
    }

    internal Dictionary<string, string> LoadFromFileText(string text)
    {
        IDeserializer deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();
        return deserializer.Deserialize<Dictionary<string, string>>(text);
    }

    private string SaveToStr(Dictionary<string, string> dictionary)
    {
        ISerializer serializer = new SerializerBuilder()
            .WithNamingConvention(CamelCaseNamingConvention.Instance)
            .Build();
        return serializer.Serialize(dictionary);
    }
}