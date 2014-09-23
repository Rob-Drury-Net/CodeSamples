using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LevelResetRegistry : MonoBehaviour 
{
	#region public variables
	public delegate void RegisterEvent(ref List<RegisterResetObject> registry);
	public static event RegisterEvent Register;
	#endregion

	#region private variables
	private List<RegisterResetObject> _registry;
	private PlanningPhase _planningPhase;
	private Inventory _inventory;
	private bool _reset;
	#endregion

	//Initializes the level for reset
	void Awake()
	{
		_registry = new List<RegisterResetObject>();
		_reset = false;
		DeathScript.ResetLevel += _beginReset;
		_planningPhase = Camera.main.GetComponent<PlanningPhase>();
		_inventory = GameObject.FindGameObjectWithTag("Player").GetComponent<Inventory>();
	}

	// Use this for initialization
	//Register the objects in the scene with the reset registry
	void Start () 
	{
		Register(ref _registry);
	}
	
	// Update is called once per frame
	// if the level should be reset run the reset function in the coroutine
	void Update () 
	{
		if(!_reset)
			return;

		StartCoroutine("Reset");
		_reset = false;
	}

	//Resets the objects to the Initial position and reinitializes the level
	public IEnumerator Reset ()
	{
		if (_inventory != null) 
		{
			_inventory.Reset();
			yield return null;
		}

		_registry.ForEach(x => x.Reset());

		if(_planningPhase != null)
			_planningPhase.BeginPlanning();
	}

	// called to begin the level reset
	private void _beginReset()
	{
		_reset = true;
	}

	// cleans up the registry after the level is ended
	void OnDisable()
	{
		_registry.Clear();
		Register = null;
	}
}
