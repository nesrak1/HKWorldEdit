using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScene : MonoBehaviour {

    // Use this for initialization
    void Start()
    {
        LoadScene();
        LoadScene();
    }
    
    void LoadScene()
    {
        if (SceneManager.GetSceneByName("blankscene1").IsValid())
        {
            Debug.Log("scene isn't null, escaping");
            return;
        }
        Scene scene = SceneManager.CreateScene("blankscene1");
        if (scene == null)
        {
            Debug.Log("scene is null");
        }
        else
        {
            Debug.Log("scene is not null");
        }
        if (scene.isLoaded)
        {
            Debug.Log("scene is loaded");
        }
        else
        {
            Debug.Log("scene is not loaded");
        }
    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
