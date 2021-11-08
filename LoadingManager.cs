using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using UnityEngine.SceneManagement;

public class LoadingManager : MonoBehaviour
{

  /*
  * LoadingManager handles all saving and loading in the game
  * This includes permanent Player requested saves, as well as temporary scene-to-scene data persistence
  * It makes the files on the player's computer and manages them as necessary
  * It's also the main controller of switching between scenes, and syncs up data from other managers as necessary 
  * Which data gets saved from which types of objects and other more specific details handled in SaveableClasses.cs
  */

    public static LoadingManager mainController;
    // used by ImportantMans , we have to make sure we don't have two LoadingMans

    public bool saveMe; // performs a scene save only (not that useful)
    public bool loadMe;
    public bool forRealSave; // performs a real save as if player had requested
    public bool deleteSaveData;
    // These four bools are for in-editor testing w/o building full debug menu

    private string permanentPath = "PermaSaveData";
    private string tempPath = "TempSaveData";
    // path names where data will be saved on machine
    // permanent save data is data that has been commited to a SAVE from the Player
    // temp save data is data that might have changed from room to room but hasn't actually been saved by the player


    private static bool USDOLRan;
    // indicates whether UpdateSaveDataOnLoad() has run
    // making this STATIC was crucial -- otherwise it reset each scene


    void Awake(){


        if (deleteSaveData){
            // if we trigger this bool while testing it will do the requested operation for us
            // sometimes its useful to delete save data before the game starts loading anything in

            Debug.Log("save data deleted");
            deleteSaveData = false;
            DeleteSaveData();
        }


        UpdateSaveDataOnLoad();
        LoadSceneData();
    }



    void Update(){
      // if we trigger one of these bools while testing it will do the requested operation for us

      if(deleteSaveData){
        Debug.Log("save data deleted  ");
        deleteSaveData = false;
        DeleteSaveData();
        TextSpeedMenu.SetTextSpeedAuto(true);

      }
      if (saveMe){
        SaveSceneData();
        saveMe = false;
      }
      if(loadMe){
        loadMe = false;
        LoadSceneData();
      }
      if(forRealSave){
        forRealSave = false;
        PlayerRequestsSave();
      }


    }

    public void DeleteSaveData(){
      // Called by player in start menu
      File.Delete(Application.persistentDataPath + "/" + permanentPath + ".dat");
      File.Delete(Application.persistentDataPath + "/" + tempPath + ".dat");
      MakeSaveData();
      MorningManager.setDay(0);
      // Because day count is static value merely recorded by save data, deleting doesn't work -- we need to manually set it here
    }

    void UpdateSaveDataOnLoad(){
      // runs once when LoadingManager comes into existence
      // creates savedata if doesn't exist and syncs up perm data and temp data for this scene

      if (!USDOLRan){
        USDOLRan = true;
        MakeSaveData();
        AllSaveData data = load<AllSaveData> (permanentPath);
        save<AllSaveData> (tempPath, data);
      }
    }

    public void PlayerRequestsSave(){
      // called mostly from player going to bed
      SaveSceneData();
      AllSaveData data = load<AllSaveData> (tempPath);
      save<AllSaveData> (permanentPath, data);
    }

    public static void createFile (string path){
      // creates file of the given name in the persistentDataPath folder

      FileStream file;
      file = File.Create(Application.persistentDataPath + "/" + path +".dat");
      file.Close();
    }

    public static void save<T>(string path, T obj){
      // these abstract save and load functions can be used to save any serializable class

      // save does not create the file, just save it
      FileStream file;
      file = File.Open(Application.persistentDataPath + "/" + path +".dat", FileMode.Open);

      BinaryFormatter bf = new BinaryFormatter();
      bf.Serialize(file, obj);
      // we use the binaryformatter to at least slightly obfuscate the save data from anyone poking around in the files
      // Lots of the chains of dependency on saved objects are kind of fragile so even a small smokescreen helps

      file.Close();
    }

    public static T load<T> (string path){
      // these abstract save and load functions can be used to save any serializable class

      FileStream file;
      BinaryFormatter bf = new BinaryFormatter();
      var data = default (T);
      if ( File.Exists  (Application.persistentDataPath + "/" + path +".dat" )){
        file = File.Open(Application.persistentDataPath + "/" + path + ".dat", FileMode.Open);
        data = (T) bf.Deserialize(file);
        file.Close();
      }

      return data;
    }

    public static T[] addItemToArray<T> (T[] myArray, T toAdd){
      // helper function used throughout project.
      // Stored here for historical reasons (used a lot in the various Saveable Classes files like AllSaveData)
      // Could have used dictionaries more often, probably. Oops!
      // ZooSim is a pretty lightweight game, so we aren't so focused on optimizing performance

      T[] newArray = new T[myArray.Length + 1];
      for (int i =0 ; i < myArray.Length; i++){
        newArray[i] = myArray[i];
      }
      newArray[myArray.Length] = toAdd;
      return newArray;
    }

    public void SaveSceneData(){
      // happens just before we load the next scene in LoadScene()
      // NOT a permanent player save, but to keep data persisting between different rooms

        SaveMe[] allData = FindObjectsOfType<SaveMe>();
        AllSaveData tempSaveSheet = load<AllSaveData>(tempPath);

        for (int i = 0; i < allData.Length; i ++){
        // run over all SaveMe objects
        allData[i].forceUpdateArrays();

        tempSaveSheet = tempSaveSheet.SaveAll(allData[i], tempSaveSheet);

      }
      save<AllSaveData>(tempPath, tempSaveSheet);
    }


    public void LoadSceneData(){
      // WE should ALWAYS load the temp data each new room. We want to fix the temp data to become
      // the default data only at Game Start time


      // NOTICE: IF A SCENE HAS NO SAVE MES, loading and saving will simply no happen.


      SaveMe[] allData = FindObjectsOfType<SaveMe>();

      AllSaveData tempSaveSheet = load<AllSaveData>(tempPath);


      for (int i = 0; i < allData.Length; i ++){ // iterate over ALL SaveMes

        allData[i].forceUpdateArrays();
        // AFTER WRITING new load for new saveMe type, don't forget to Instantiate a new ActionData in MakeSaveData.

        tempSaveSheet.LoadAll(allData[i], tempSaveSheet);

      }


      if (allData.Length == 0){
        Debug.Log("WARNING!!! NO SAVE MES IN THIS SCENE!! LOADING BROKEN!!");
      }

    }

    public void MakeSaveData(){

      // these two create the save datas as needed

      if (!File.Exists(Application.persistentDataPath + "/" + tempPath +".dat") ){

        createFile(tempPath);
        AllSaveData dataDirectory = new AllSaveData();
        dataDirectory.SetInitialRefernces();



        save<AllSaveData> (tempPath, dataDirectory);
      }

      if (!File.Exists(Application.persistentDataPath + "/" + permanentPath + ".dat") ){

        createFile(permanentPath);
        AllSaveData dataDirectory = new AllSaveData();

        save<AllSaveData> (permanentPath, dataDirectory);
      }
    }

    public void LoadScene(string scene){
      // called when player moves between rooms, such as by LoadingZone

      TransitionFader fader = FindObjectOfType(typeof(TransitionFader)) as TransitionFader;
      // the object that fades our screen to black when we switch rooms

      SaveSceneData();
      StartCoroutine(WaitForFade(fader, scene) );
    }

    IEnumerator WaitForFade(TransitionFader fader, string sceneName) {
      // WaitForFade delays until both music and the TransitionFader have completed
      // called by LoadScene()

      // technically you should probably add wait for sfxFade as well?
      // But currently I don't think this comes up anywhere
        SFXMan sfxman = FindObjectOfType(typeof(SFXMan)) as SFXMan;
        fader.fadeMeOut();
        bool wait = fader.fadingActive || MusicMan.GetFadingMusicNow();




        while (wait ){
            yield return new WaitForSeconds(.05f);
            wait = fader.fadingActive || MusicMan.GetFadingMusicNow() ;
        }

        SceneManager.LoadScene(sceneName); // ask unity to load the next room


    }



    public void IncrementDaysSinceData(){
      // called by DaysManager in a specific moment of the day/night loop.
      // Increments the game's day count
      AllSaveData tempSaveSheet = load<AllSaveData>(tempPath);

      SaveMe[] allData = FindObjectsOfType<SaveMe>();
      tempSaveSheet.IncrementAllDaySinceData(tempSaveSheet, allData);

      save<AllSaveData>(tempPath, tempSaveSheet);


    }
}
