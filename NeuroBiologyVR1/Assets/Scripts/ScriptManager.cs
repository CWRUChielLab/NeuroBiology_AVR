﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using HoloToolkit.Sharing;
using HoloToolkit.Sharing.Tests;

public class ScriptManager : MonoBehaviour {

    public GameObject
        gui_canvas, rec_electrode, color_cube, player_obj, player_cam,
        plus_toggle, minus_toggle;
    public GameObject
        pause_label, plus_label, minus_label;

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
    public bool rec_enabled = false;
    private bool isPaused = false;

    private bool spacePausing = false;

    public int band_width = 20;

    private float last_update;
    private double since_stimulation = 0.01;

    private double time_scale = 0.00002;         //seconds * time_scale = simulated seconds
    private double time_converter = 20;          //seconds * time_converter = simulated microseconds

    private int num_points = 25;    //was 55
    private int pointBuffer = 4;       //pointBuffer * num_points = number of points stored in buffer
    private bool analysis_mode = false;

    private bool audio_isPlaying=false;

    public int e_pos = 30;
    public bool spacePause;

    private float diam_val=1, max_diam=3, min_diam=1;

    private Vector3 vec_oldPos = new Vector3(3.7f, 119.6f, -147.5f);    //position Vector at x=30;

    private Vector3 updatedLocalPos;

    public bool isMultiplayer;

    public bool passedTransform { get; private set; }

    //Current localhost for copy/paste: 169.254.80.80

    // Use this for initialization
    void Start () {
        //for Sharing
        if (isMultiplayer)
        {
            CustomMessages.Instance.MessageHandlers[CustomMessages.TestMessageID.HeadTransform] = this.OnShareElectrode;
            
            //SharingSessionTracker.Instance.SessionJoined += this.OnSessionJoined;

        }

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

	}
	
    private void OnSessionJoined(object sender)
    {
        passedTransform = false;
        if (passedTransform)
        {
            CustomMessages.Instance.SendElectrodeTransform(rec_electrode.transform.localPosition);
        }
    }

    public void OnShareElectrode(NetworkInMessage msg)
    {
        long userID = msg.ReadInt64();

        updatedLocalPos = CustomMessages.Instance.ReadVector3(msg);

        Debug.Log(updatedLocalPos);

        passedTransform = true;
    }

    public void SendUpdatedElectrode(Vector3 newLocPos)
    {
        passedTransform = true;
        updatedLocalPos = newLocPos;
        CustomMessages.Instance.SendElectrodeTransform(newLocPos);
    }

    private bool tempTestBool = true;

	// Update is called once per frame
	void Update () {
        if (passedTransform)
        {
            rec_script.SetLocalPos(updatedLocalPos);
            passedTransform = false;
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if(spacePause)
                TogglePause();
            else if (tempTestBool && graph_enabled)
            {
                Vector3 tVec = rec_electrode.transform.localPosition + new Vector3(0f, -50f, 0f);
                Debug.Log(tVec);
                SendUpdatedElectrode(tVec);
            }
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
        }
        isPaused = !isPaused;
        return isPaused;
    }

    public void Stimulate()
    {
        since_stimulation = 0.01;
        for(int i = 0; i < particle_spheres.Length; i++)
        {
            particle_spheres[i].GetComponent<ParticleSystem>().Emit(100);
        }
        audio_obj.GetComponent<AudioSource>().Play();
        audio_isPlaying = true;
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
    }

}
