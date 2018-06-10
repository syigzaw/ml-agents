using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class GameController : MonoBehaviour {

	public GameObject titlePanel;
	public GameObject backButton;
	public GameObject stickTitleScreen;
	public ThrowBone throwController;
	public CinemachineVirtualCamera cameraTitle;
	public CinemachineVirtualCamera cameraGame;
	public CinemachineBrain cmBrain;
	// Use this for initialization
	void Awake () {
		throwController = GetComponent<ThrowBone>();
		cmBrain = FindObjectOfType<CinemachineBrain>();
		throwController.enabled = false;
		throwController.bone.gameObject.SetActive(false);
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void StartGame()
	{
		titlePanel.SetActive(false);
		backButton.SetActive(true);
		cameraTitle.Priority = 1;
		cameraGame.Priority = 2;
		throwController.enabled = true;
		stickTitleScreen.SetActive(false);
		throwController.bone.gameObject.SetActive(true);


	}

	public void EndGame()
	{
		titlePanel.SetActive(true);
		backButton.SetActive(false);
		cameraTitle.Priority = 2;
		cameraGame.Priority = 1;
		throwController.enabled = false;
		stickTitleScreen.SetActive(true);
		throwController.bone.gameObject.SetActive(false);

	}
}
