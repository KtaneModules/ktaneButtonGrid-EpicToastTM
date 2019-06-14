using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using KModkit;

public class ButtonGridsScript : MonoBehaviour {

    public KMBombModule Module;
    public KMAudio Audio;
    public KMBombInfo Bomb;
    public KMSelectable[] btnSelectables;
    public KMSelectable resetSelectable;
    public MeshRenderer[] btnRenderers, stageRenderers;
    public Material[] btnMaterials, stageMaterials;

    private static int _moduleIdCounter = 1;
    private int _moduleId;
    private bool solved = false;

    private int[,] colorSolution = new int[10, 4]; // 0 = red, 1 = blue, 2 = yellow, 3 = green
    private int[] colors = { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3 };
    private bool[] pressed = new bool[40];
    private readonly static string[] colorNames = { "red", "blue", "yellow", "green" };
    private bool unicornApplies = false;

    int stageNum = 0, ministageNum = 0;
    int[] pressedStage = new int[4];
    int[] btnsPressedThisStage = { 99, 99, 99, 99 };

    // Use this for initialization
    void Start () {
        _moduleId = _moduleIdCounter++;
        Module.OnActivate += Activate;

        GenerateModule();
    }

    void Activate()
    {
        for (int i = 0; i < btnSelectables.Length; i++)
        {
            int j = i;
            btnSelectables[i].OnInteract += delegate ()
            {
                if (!solved)
                    BtnPress(j);
                Audio.PlaySoundAtTransform("ButtonPress", Module.transform);
                btnSelectables[j].AddInteractionPunch();
                return false;
            };
        }

        resetSelectable.OnInteract += delegate ()
        {
            if (!solved)
            {
                DebugMsg("Resetting...");
                GenerateModule();
            }
            Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.LightBuzz, Module.transform);
            resetSelectable.AddInteractionPunch(10);
            return false;
        };
    }

    void GenerateModule()
    {
        stageNum = 0;
        ministageNum = 0;

        colors = colors.Shuffle();
        for (int x = 0; x < 40; x++)
        {
            btnRenderers[x].material = btnMaterials[colors[x]];
            pressed[x] = false;
        }

        for (int i = 0; i < 10; i++)
            stageRenderers[i].material = stageMaterials[0];

        if (Bomb.IsIndicatorOn("BOB") && Bomb.IsPortPresent(Port.DVI) && Bomb.GetBatteryCount() == 1 && Bomb.GetSerialNumberLetters().All(x => x == 'C' || x == 'H' || x == 'R' || x == 'I' || x == 'S'))
        {
            DebugMsg("The unicorn applies.");
            unicornApplies = true;
        }

        if (Bomb.IsIndicatorPresent("SIG"))
            AddToSolution(new int[] { 0, 1, 2, 3 }, 0);
        else if (Bomb.IsIndicatorPresent("TRN"))
            AddToSolution(new int[] { 3, 0, 2, 1 }, 0);
        else if (Bomb.IsIndicatorPresent("FRQ"))
            AddToSolution(new int[] { 0, 2, 1, 3 }, 0);
        else
            AddToSolution(new int[] { 3, 2, 1, 0 }, 0);

        if (Bomb.GetModuleNames().Count() > 19)
            AddToSolution(new int[] { 1, 0, 2, 3 }, 1);
        else if (Bomb.GetModuleNames().Count() > 14)
            AddToSolution(new int[] { 1, 3, 2, 0 }, 1);
        else if (Bomb.GetModuleNames().Count() > 9)
            AddToSolution(new int[] { 3, 1, 0, 2 }, 1);
        else
            AddToSolution(new int[] { 3, 0, 1, 2 }, 1);

        if (CheckInSerial("BTNGWD"))
            AddToSolution(new int[] { 2, 1, 0, 3 }, 2);
        else if (CheckInSerial("JVPKZF"))
            AddToSolution(new int[] { 2, 3, 1, 0 }, 2);
        else if (CheckInSerial("AEIOU"))
            AddToSolution(new int[] { 1, 3, 0, 2 }, 2);
        else
            AddToSolution(new int[] { 1, 0, 3, 2 }, 2);

        if (Bomb.GetBatteryCount() > 5)
            AddToSolution(new int[] { 0, 2, 3, 1 }, 3);
        else if (Bomb.GetBatteryCount() > 3)
            AddToSolution(new int[] { 0, 3, 1, 2 }, 3);
        else if (Bomb.GetBatteryCount() == Bomb.GetBatteryHolderCount())
            AddToSolution(new int[] { 2, 0, 3, 1 }, 3);
        else
            AddToSolution(new int[] { 2, 0, 1, 3 }, 3);

        if (Bomb.IsPortPresent(Port.Parallel))
            AddToSolution(new int[] { 0, 1, 3, 2 }, 4);
        else if (Bomb.IsPortPresent(Port.PS2))
            AddToSolution(new int[] { 1, 2, 0, 3 }, 4);
        else if (Bomb.GetPortCount() == 0)
            AddToSolution(new int[] { 3, 2, 0, 1 }, 4);
        else
            AddToSolution(new int[] { 2, 1, 3, 0 }, 4);

        bool ruleApplied = false;
        for (int i = 0; i < 8; i++)
        {
            int[] column = { colors[i], colors[i + 8], colors[i + 16], colors[i + 24], colors[i + 32] };

            if (column.Distinct().Count() == 4)
            {
                AddToSolution(new int[] { 3, 2, 0, 1 }, 5);
                ruleApplied = true;
                break;
            }
        }

        if (!ruleApplied)
        {
            for (int i = 0; i < 5; i++)
            {
                int[] row = { colors[i * 8], colors[i * 8 + 1], colors[i * 8 + 2], colors[i * 8 + 3], colors[i * 8 + 4], colors[i * 8 + 5], colors[i * 8 + 6], colors[i * 8 + 7] };

                if (row.Distinct().Count() == 3)
                {
                    AddToSolution(new int[] { 1, 0, 3, 2 }, 5);
                    ruleApplied = true;
                    break;
                }
            }
        }

        if (!ruleApplied)
        {
            if (colors[0] == colors[7] && colors[7] == colors[32] && colors[32] == colors[39])
                AddToSolution(new int[] { 2, 3, 1, 0 }, 5);

            else if (!ruleApplied)
                AddToSolution(new int[] { 0, 1, 2, 3 }, 5);
        }

        int[] firstRow = { colors[0], colors[1], colors[2], colors[3], colors[4], colors[5], colors[6], colors[7] };
        int[] occurrences = { 0, 0, 0, 0 };
        foreach (var color in firstRow)
        {
            occurrences[color]++;
        }

        if (occurrences[1] > occurrences[2] && occurrences[1] > occurrences[0] && occurrences[1] > occurrences[3])
            AddToSolution(new int[] { 1, 3, 2, 0 }, 6);
        else if (occurrences[3] > occurrences[2] && occurrences[3] > occurrences[0] && occurrences[3] > occurrences[1])
            AddToSolution(new int[] { 3, 0, 1, 2 }, 6);
        else if (occurrences[0] > occurrences[2] && occurrences[0] > occurrences[3] && occurrences[0] > occurrences[1])
            AddToSolution(new int[] { 0, 2, 3, 1 }, 6);
        else
            AddToSolution(new int[] { 2, 1, 0, 3 }, 6);

        int[] firstCol = { colors[0], colors[8], colors[16], colors[24], colors[32] };
        occurrences[0] = 0;
        occurrences[1] = 0;
        occurrences[2] = 0;
        occurrences[3] = 0;
        foreach (var color in firstCol)
        {
            occurrences[color]++;
        }
        
        if (occurrences[0] == 2)
            AddToSolution(new int[] { 0, 3, 1, 2 }, 7);
        else if (occurrences[2] == 2)
            AddToSolution(new int[] { 2, 0, 3, 1 }, 7);
        else if (occurrences[3] == 2)
            AddToSolution(new int[] { 3, 1, 2, 0 }, 7);
        else
            AddToSolution(new int[] { 1, 2, 0, 3 }, 7);

        if (colors[39] == 1)
            AddToSolution(new int[] { 0, 3, 2, 1 }, 8);
        else if (colors[39] == 0)
            AddToSolution(new int[] { 2, 1, 3, 0 }, 8);
        else if (colors[39] == 2)
            AddToSolution(new int[] { 3, 1, 0, 2 }, 8);
        else
            AddToSolution(new int[] { 1, 0, 2, 3 }, 8);
    }

    void AddToSolution(int[] colors, int stageNum)
    {
        for (int i = 0; i < 4; i++)
        {
            colorSolution[stageNum, i] = colors[i];
        }

        DebugMsg("The order for Stage " + (stageNum + 1) + " is " + colorNames[colorSolution[stageNum, 0]] + ", " + colorNames[colorSolution[stageNum, 1]] + ", " + colorNames[colorSolution[stageNum, 2]] + ", " + colorNames[colorSolution[stageNum, 3]] + ".");
    }

    bool CheckInSerial(string stringThatYknowYouWantToCheckIfSaidStringIsPresentInTheBombsSerialNumber)
    {
        bool presentInSerial = false;
        
        foreach (var letter in stringThatYknowYouWantToCheckIfSaidStringIsPresentInTheBombsSerialNumber)
        {
            if (Bomb.GetSerialNumber().Contains(letter))
            {
                presentInSerial = true;
                break;
            }
        }

        return presentInSerial;
    }

    void BtnPress(int btnNum)
    {
        if (ministageNum == 0)
            for (int i = 0; i < 4; i++)
                btnsPressedThisStage[i] = 99;
        
        btnsPressedThisStage[ministageNum] = btnNum;

        if (pressed[btnNum])
        {
            Module.HandleStrike();
            DebugMsg("You pressed a button that was already pressed. STRIKE!");
            ministageNum = 0;

            for (int i = 0; i < 4; i++)
                if (btnsPressedThisStage[i] != 99)
                    pressed[btnsPressedThisStage[i]] = false;
        }
        else
        {
            pressedStage[ministageNum] = colors[btnNum];
            pressed[btnNum] = true;
            DebugMsg("You pressed the " + colorNames[pressedStage[ministageNum]] + " button.");

            ministageNum++;
        }

        int[] unicorn = { 1, 0, 1, 2 };

        if (ministageNum == 4)
        {
            bool nopeThatsWrong = false;
            for (int i = 0; i < 4; i++)
            {
                if (pressedStage[i] != colorSolution[stageNum, i])
                {
                    nopeThatsWrong = true;
                    break;
                }
            }

            if (stageNum == 0 && pressedStage[0] == unicorn[0] && pressedStage[1] == unicorn[1] && pressedStage[2] == unicorn[2] && pressedStage[3] == unicorn[3] && unicornApplies)
            {
                Module.HandlePass();
                DebugMsg("The unicorn rule applied. Solving module...");

                for (int i = 0; i < 40; i++)
                {
                    btnRenderers[i].material = btnMaterials[3];
                }
            }
            
            else if (nopeThatsWrong && stageNum != 9)
            {
                Module.HandleStrike();
                DebugMsg("Your submission for Stage " + (stageNum + 1) + " was wrong. STRIKE!");
                for (int i = 0; i < 4; i++)
                    if (btnsPressedThisStage[i] != 99)
                        pressed[btnsPressedThisStage[i]] = false;
            }
            else
            {
                stageNum++;
                stageRenderers[stageNum - 1].material = stageMaterials[1];

                if (stageNum != 10)
                    DebugMsg("Your submission for Stage " + stageNum + " was correct. Moving on...");
                else
                {
                    Module.HandlePass();
                    DebugMsg("Your submission for Stage 10 was correct. Solving module...");

                    for (int i = 0; i < 40; i++)
                    {
                        btnRenderers[i].material = btnMaterials[3];
                    }
                }
            }

            ministageNum = 0;
        }
    }

    void DebugMsg(string msg)
    {
        Debug.LogFormat("[Button Grids #{0}] {1}", _moduleId, msg);
    }

    public string TwitchHelpMessage = "Use !{0} press 1 2 3 4 or !{0} 1 2 3 4 to press the buttons 1 2 3 4 in reading order. You can press any number of buttons you want! Press all forty of them at once if you want, I don't care. You can do !{0} reset to reset.";
    IEnumerator ProcessTwitchCommand(string cmd)
    {
        if (cmd.StartsWith("press "))
        {
            cmd = cmd.Substring(6);
            DebugMsg(cmd);
        }

        else if (cmd == "reset")
        {
            yield return null;
            yield return new KMSelectable[] { resetSelectable };
            yield break;
        }

        string[] numbers = cmd.Split(' ');

        int numbersProcessed = 0;
        int result = 0;

        foreach (var number in numbers)
        {
            if (int.TryParse(number, out result))
            {
                yield return null;
                yield return new KMSelectable[] { btnSelectables[int.Parse(number) - 1] };
                numbersProcessed++;
            }

            else
            {
                yield return "sendtochaterror The " + (numbersProcessed + 1) + "th button in your command is not a number.";
            }
        }
    }
}
