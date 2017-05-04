﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
//using HoloToolkit.Sharing;
//using HoloToolkit.Sharing.Tests;
using HoloToolkit.Unity.InputModule;
using UnityEngine.Windows.Speech;
using System.Linq;

public class ScriptManager : MonoBehaviour {

    public GameObject
        gui_canvas, rec_electrode, color_cube, player_obj, player_cam,
        plus_toggle, minus_toggle;
    public GameObject
        pause_label, plus_label, minus_label, particle_label, voice_label;

    public GameObject[] initial_toggles, secondary_toggles;

    public TransformUp stim_tran, scale_tran, prompt_tran;

    public GameObject audio_obj;

    public GameObject[] particle_spheres;

    public WMGTest graph_script;
    public ElectrodeBehavior rec_script;
    public ChangeColor color_script;
    public FrequencyChange audio_script;
    public BandsScaling band_script;

    public Material bandMat_high;
    
    private bool graph_enabled = false;
    private bool rec_enabled = false;
    private bool isPaused = false;

    private bool spacePausing = false;

    public int band_width = 20;

    private float last_update;
    private double since_stimulation = 0.01;

    private double time_scale = 0.00002;         //seconds * time_scale = simulated seconds
    private double time_converter = 20;          //seconds * time_converter = simulated microseconds

    private int num_points = 25;    //25 for hololens, 55 for pc
    private int pointBuffer = 4;       //pointBuffer * num_points = number of points stored in buffer
    private bool analysis_mode = false;

    private bool audio_isPlaying=false;

    private bool voiceEnabled = false;

    public int e_pos = 30;
    public bool spacePause;

    public bool enableParticles;

    private float diam_val=1, max_diam=3, min_diam=1;

    private Vector3 vec_oldPos = new Vector3(3.7f, 119.6f, -147.5f);    //position Vector at x=30;

    private KeywordRecognizer voice_command;
    private Dictionary<string, System.Action> keywords;

    public bool isMultiplayer;
    public bool changedComponent { get; private set; }      //true if any component has been updated

    //true if the simulation should pause, stimulate, toggle graph on, rescale, toggle mode to analysis, or update diameter
    private bool passedPause, passedStim, passedGraph, passedRescale, passedMode, passedDiameter;
    


    public bool passedTransform { get; private set; }       //toggled for electrode position update
    private bool updatedBool;
    private short updatedShort;
    private Vector3 updatedVec3;


    //Current localhost for copy/paste: 169.254.80.80

    // Use this for initialization
    void Start () {
        //for Sharing
        /*if (isMultiplayer)
        {
            CustomMessages.Instance.MessageHandlers[CustomMessages.TestMessageID.HeadTransform] = this.ReceiveMessage;
            SharingStage.Instance.SessionsTracker.UserJoined += OnSessionJoined;
            //SharingSessionTracker.Instance.SessionJoined += this.OnSessionJoined;

        }*/

        //get scripts from GameObjects
        graph_script = gui_canvas.GetComponent<WMGTest>();
        rec_script = rec_electrode.GetComponent<ElectrodeBehavior>();
        color_script = color_cube.GetComponent<ChangeColor>();
        audio_script = audio_obj.GetComponent<FrequencyChange>();

        color_script.bandMat_high = bandMat_high;
        //calculate voltage
        color_script.time_scale = time_scale;
        //init graph
        graph_script.num_points = num_points;
        graph_script.band_width = band_width;
        graph_script.pointBuffer = pointBuffer;

        //graph_script.SetGraph(graph_enabled);
        //graph_script.set_recEnabled(rec_enabled);

        keywords = new Dictionary<string, System.Action>();

        keywords.Add("pause", () =>
        {
            if (!isPaused)
                TogglePause();
        });
        keywords.Add("resume", () =>
        {
            if (isPaused)
                TogglePause();
        });

        voice_command = new KeywordRecognizer(keywords.Keys.ToArray());
        voice_command.OnPhraseRecognized += Voice_command_OnPhraseRecognized;
        //voice_command.Start();
	}

    private void Voice_command_OnPhraseRecognized(PhraseRecognizedEventArgs args)
    {
        System.Action resultAction;
        if(keywords.TryGetValue(args.text, out resultAction))
        {
            resultAction.Invoke();
        }
    }

    public void ExitApplication()
    {
        Application.Quit();
    }

    public void ToggleParticles()
    {
        enableParticles = !enableParticles;
        if (enableParticles)
        {
            particle_label.GetComponent<Text>().text = "Particles: On";
        }
        else
        {
            particle_label.GetComponent<Text>().text = "Particles: Off";
        }
    }

    public void ToggleVoice()
    {
        voiceEnabled = !voiceEnabled;
        if (voiceEnabled)
        {
            voice_command.Start();
            voice_label.GetComponent<Text>().text = "Voice Control: On";
        }
        else
        {
            voice_command.Stop();
            voice_label.GetComponent<Text>().text = "Voice Control: Off";
        }
    }

    /*
    private void OnSessionJoined(Session session, User user)
    {
        int numUsers = session.GetUserCount();
        
        passedTransform = false;
        if (passedTransform)
        {
            CustomMessages.Instance.SendElectrodeTransform(rec_electrode.transform.localPosition);
        }
    }

    public void ReceiveMessage(NetworkInMessage msg)
    {
        long userID = msg.ReadInt64();

        short message_type = msg.ReadInt16();

        switch (message_type)
        {
            case 0:     //exit byte
                break;
            case 1:     //pause byte
                passedPause = true;
                updatedBool = (msg.ReadByte() == 1);
                break;
            case 2:     //stimulate byte
                passedStim = true;
                updatedBool = (msg.ReadByte() == 1);
                break;
            case 3:     //electrode localpos vec3 (and maybe enabled bool later)
                updatedVec3 = CustomMessages.Instance.ReadVector3(msg);
                //Debug.Log(updatedLocalPos);
                passedTransform = true;
                break;
            case 4:     //graph enabled byte
                passedGraph = true;
                updatedBool = (msg.ReadByte() == 1);
                break;
            case 5:     //rescale byte
                passedRescale = true;
                updatedBool = (msg.ReadByte() == 1);
                break;
            case 6:     //analysis mode byte
                passedMode = true;
                updatedBool = (msg.ReadByte() == 1);
                break;
            case 7:     //diameter value short
                passedDiameter = true;
                updatedShort = msg.ReadInt16();
                break;
            default:
                break;
        }

        changedComponent = true;
    }

    public void SendUpdatedElectrode(Vector3 newLocPos)
    {
        passedTransform = true;
        updatedVec3 = newLocPos;
        CustomMessages.Instance.SendVec3(3, newLocPos);
    }
    */
    //private bool tempTestBool = true;

    // Update is called once per frame
    void Update () {
        /*
        if (changedComponent)
        {
            if (passedPause)
            {
                TogglePause();
                passedPause = false;
            }
            if (passedStim)
            {
                Stimulate();
                passedStim = false;
            }
            if (passedTransform)
            {
                rec_script.SetLocalPos(updatedVec3);
                passedTransform = false;
            }
            if (passedGraph)
            {
                ToggleGraph();
                passedGraph = false;
            }
            if (passedRescale)
            {
                RescaleGraph();
                passedRescale = false;
            }
            if (passedMode)
            {
                ToggleMode();
                passedMode = false;
            }
            if (passedDiameter)
            {
                SetDiameter(updatedShort);
                passedDiameter = false;
            }
            changedComponent = false;
        }*/
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(spacePause)
                TogglePause();
        }
        else if (isPaused)
            return;
        if (since_stimulation == 0.01)     //continuous stimulation framework
        {
            last_update = Time.deltaTime;
            //double[] voltage_data = color_script.UpdateVoltage(0.000001);
            //graph_script.AddPoint(voltage_data, (float)(last_update * time_converter));
            since_stimulation += Time.deltaTime;
        }
        else
        {
            last_update = Time.deltaTime;
            double total_time = since_stimulation + last_update;

            double[] voltage_data = color_script.UpdateVoltage(total_time * time_scale);
            color_script.ColorBands(voltage_data);
            graph_script.AddPoint(voltage_data, (float)(last_update*time_converter));
            since_stimulation = total_time;

            double temp_volt = voltage_data[e_pos];

            if (audio_isPlaying && temp_volt < 0.02)
            {
                audio_obj.GetComponent<AudioSource>().Stop();
                audio_isPlaying = false;
            }
            else
            {
                if (rec_enabled)
                    audio_script.PassData(temp_volt);
                else
                    audio_script.PassData(voltage_data[band_width]);
            }
        }
	}


    void SetVars()     //set variables to predetermined values
    {

    }

    public void ToggleGraph()
    {
        graph_enabled = !graph_enabled;
        for(int i = 0; i < initial_toggles.Length; i++)
        {
            initial_toggles[i].SetActive(!graph_enabled);
        }
        for (int i = 0; i < secondary_toggles.Length; i++)
        {
            secondary_toggles[i].SetActive(graph_enabled);
        }

        graph_script.ToggleGraph();
        //if (!passedGraph)
        //    CustomMessages.Instance.SendByte(4, 1);
    }

    public bool TogglePause()       //returns whether the time is NOW paused -> after calling TogglePause
    {
        color_cube.SetActive(isPaused);
        if (!isPaused)
        {
            Time.timeScale = 0;
            graph_script.enabled = false;
            color_script.enabled = false;
            rec_script.enabled = false;
            pause_label.GetComponent<Text>().text = "Resume";
            pause_label.GetComponent<Text>().color = Color.red;
            audio_obj.GetComponent<AudioSource>().Stop();
            audio_isPlaying = false;
        }
        else
        {
            Time.timeScale = 1;
            graph_script.enabled = true;
            color_script.enabled = true;
            rec_script.enabled = true;
            pause_label.GetComponent<Text>().text = "Pause";
            pause_label.GetComponent<Text>().color = Color.white;
        }
        isPaused = !isPaused;
        //if (!passedPause)
        //    CustomMessages.Instance.SendByte(1, 1);     
        return isPaused;
    }

    public void Stimulate()
    {
        since_stimulation = 0.01;
        if (enableParticles)
        {
            for (int i = 0; i < particle_spheres.Length; i++)
            {
                particle_spheres[i].GetComponent<ParticleSystem>().Emit(100);
            }
        }
        audio_obj.GetComponent<AudioSource>().Play();
        audio_isPlaying = true;
        //if (!passedStim)
        //    CustomMessages.Instance.SendByte(2, 1);
    }

    public void UpdateElectrode(int newEPos)
    {
        rec_enabled = true;
        e_pos = newEPos;
        color_script.ResetBand(e_pos);
        graph_script.RecieveSliderValue(e_pos);
        graph_script.set_recEnabled(true);
    }

    public void SetDiameter(float value)
    {
        //Debug.Log(diam_val);
        if (diam_val + value > max_diam || diam_val + value < min_diam)
            return;
        diam_val += value;
        color_script.SetVariable(diam_val);
        band_script.SetBandScale(diam_val);
        stim_tran.SetTransformUp(diam_val);
        rec_script.UpdateYPos(diam_val);
        rec_script.TransformAdjust(diam_val);
        scale_tran.SetTransformUp(diam_val);
        prompt_tran.SetTransformUp(diam_val);

        

        //Debug.Log(diam_val);

        if (diam_val == max_diam)
        {
            //set plus to grey
            plus_label.GetComponent<Text>().color = Color.gray; //new Color(90f, 90f, 90f, 250f);
            plus_toggle.SetActive(false);
        } else if (diam_val == min_diam)
        {
            //set minus to grey
            minus_label.GetComponent<Text>().color = Color.gray;
            minus_toggle.SetActive(false);
        } else
        {
            plus_label.GetComponent<Text>().color = new Color(250, 250, 250);
            minus_label.GetComponent<Text>().color = new Color(250, 250, 250);

            plus_toggle.SetActive(true);
            minus_toggle.SetActive(true);

        }

        //if (!passedDiameter)
        //    CustomMessages.Instance.SendShort(7, (short)diam_val);
    }

    public void RescaleGraph()
    {

        float newMax;

        switch ((int)diam_val)
        {
            case 1:
                newMax = 1f;
                break;
            case 2:
                newMax = 0.35f;
                break;
            case 3:
                newMax = 0.17f;
                break;
            default:
                newMax = 1f;
                break;
        }

        graph_script.Rescale_Voltage(newMax);

        //if (!passedRescale)
        //    CustomMessages.Instance.SendByte(5, 1);
    }

    public void DisableElectrode()
    {
        rec_enabled = false;
        graph_script.set_recEnabled(false);
    }

    public void ToggleMode()
    {
        analysis_mode = !analysis_mode;
        //graph_script.pointBuffer = pointBuffer;
        graph_script.ToggleMode();
        if ((analysis_mode && !isPaused) || (!analysis_mode && isPaused))
        {
            TogglePause();
        }

        //if (!passedMode)
        //    CustomMessages.Instance.SendByte(6, 1);
    }

    public void OnSpeechKeywordRecognized(SpeechKeywordRecognizedEventData eventData)
    {
        
    }
}
