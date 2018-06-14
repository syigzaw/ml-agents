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

	//////////////////////////////////////////////////////////////////////////
	[SpaceAttribute(20)]        
	[Header("Music & Sound Effects")]
	public AudioSource audioSourceBackgroundMusic;
	public AudioSource audioSourceSFX;
	public bool playBackgroundMusic = true;
	public List<AudioClip> gameStartingSounds = new List <AudioClip>();
	public List<AudioClip> wonRoundSounds = new List <AudioClip>();
	public List<AudioClip> wonGameSounds = new List <AudioClip>();
	public List<AudioClip> scoredPointSounds = new List <AudioClip>();
	// public List<AudioClip> backgroundMusic = new List <AudioClip>();
	public AudioClip backgroundMusic;
	public AudioClip buttonClickStartSFX;
	public AudioClip buttonClickEndSFX;

	////////////////////////////////////////////////////////////////////////////


	void SetupAudio()
	{
		audioSourceBackgroundMusic = gameObject.AddComponent<AudioSource>();
		audioSourceSFX = gameObject.AddComponent<AudioSource>();
		audioSourceBackgroundMusic.loop = true;
		audioSourceBackgroundMusic.volume = .5f;
	}

	//plays when game starts
	public void PlayGameStartingAudio()
	{
		foreach (AudioClip clip in gameStartingSounds)
		{
			audioSourceSFX.PlayOneShot(clip, 1);
		}
	}

	public void PlayGameBackgroundAudio()
	{
		{
			// audioSourceBackgroundMusic.clip = backgroundMusic[Random.Range( 0, backgroundMusic.Count)];
			audioSourceBackgroundMusic.clip = backgroundMusic;
			audioSourceBackgroundMusic.Play();
		}
	}


	//plays when game is completely over
	public void PlayGameDoneAudio()
	{
		foreach (AudioClip clip in wonGameSounds)
		{
			audioSourceSFX.PlayOneShot(clip);
		}
	}



	// Use this for initialization
	void Awake () {
		throwController = GetComponent<ThrowBone>();
		cmBrain = FindObjectOfType<CinemachineBrain>();
		throwController.enabled = false;
		throwController.bone.gameObject.SetActive(false);
		SetupAudio();
		if (playBackgroundMusic)
		{
			PlayGameBackgroundAudio();
		}
		
	}
	
	public void StartGame()
	{
		audioSourceSFX.PlayOneShot(buttonClickStartSFX, 1);

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
		audioSourceSFX.PlayOneShot(buttonClickEndSFX, 1);
		titlePanel.SetActive(true);
		backButton.SetActive(false);
		cameraTitle.Priority = 2;
		cameraGame.Priority = 1;
		throwController.bone.gameObject.SetActive(false);
		throwController.dog.target = throwController.returnPoint;
		throwController.enabled = false;
		stickTitleScreen.SetActive(true);

	}
}
