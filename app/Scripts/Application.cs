// Application.cs
//
// Implements the Application class, the central manager and authority on game logic.
using Godot;
using System;
using System.Text;



public class Application : Node
{

// These four values should be included in Types.cs or similar.
const int gridHeight = 15;
const int gridWidth = 41;
const int mapHeight = 30;
const int mapWidth = 102;


// The state in which the game (should be) at.
// Both are waiting for user input; the difference is what the input should address.
public enum State {
	WAIT_CHOICE_NODE,
	WAIT_CHOICE_EVENT
}
// No body for neither the constructor nor the destructor
//public Application() {}
//~Application() {}


// Given the file path to the map.txt, return the concatenated 
// string representation of the map.
public string readMapFile(string mapFilePath) {
	File file = new File();
	file.Open(mapFilePath, File.ModeFlags.Read);
	string text = file.GetAsText();
	file.Close();

	return text;
}


// Initializes game resources; calls appropriate functions to read from files and initialize nodes, events, and map.
public override void _Ready() {
	// call files...
	// Init Nodes
	// Init Events
	// read the map!
	try{
	
	m_state = State.WAIT_CHOICE_NODE;
	
	m_node = GetNode("UIManager") as Godot.Object;
	
	string fileContents = readMapFile("res://data/map.txt");
	
	int index = 0;
	m_map = new char[mapHeight, mapWidth];
	for (int row = 0; row < mapHeight; ++row) {
		for (int col = 0; col < mapWidth; ++col) {
			m_map[row, col] = fileContents[index];
			++index;
		}
		++index;
	}
	
	// Load events
	json json_node = GetNode("JSONController") as json;
	int num_events;
	int num_nodes;
	m_events = json_node.LoadEvents("res://data/events.json", out num_events);
	m_nodes = json_node.LoadSeaNodes("res://data/nodes.json", out num_nodes);
	
	
	m_eventWeights = new float[num_events];
	m_totalWeight = 0.0f;
	for (int i = 0; i < num_events; ++i) {
		m_eventWeights[i] = m_totalWeight;
		m_totalWeight += m_events[i].GetProbability();
	}
	
	m_random = new Random();
	
	PrintMap();
	}
	catch(Exception e) {GD.Print(e);}
}

int RandomEvent() {
	float randomNumber = (float) m_random.NextDouble() * m_totalWeight;
	int eventIndex = 0;
	while (eventIndex < m_nodes.Length - 1 && m_eventWeights[eventIndex + 1] < randomNumber) {
		++eventIndex;
	}
	return eventIndex;
}

// UserInput(int)
// Handles the given user input, received as an integer input.
// =============================================
// input:	User's input.
public void UserInput(int input) {
	if (m_state == State.WAIT_CHOICE_NODE) {
		NodeChoiceHandler(input - 1);
	}
	else if (m_state == State.WAIT_CHOICE_EVENT) {
		EventChoiceHandler(input - 1);
	}
}


// NodeChoiceHandler(int)
// Handles the given user choice, received as an integer input.
// Invalid choices will be ignored.
// =============================================
// choice:	User's choice.
private void NodeChoiceHandler(int choice) {
	// TODO: add option for resting, and other additional options that can be expected from a sea node.
	if (choice < 0) {
		return;
	}

	int[] adjList = m_nodes[m_nodeId].GetAdjacencyList(); 
	if (choice < adjList.Length) {
		TravelEdge(adjList[choice]);
	}
	// option after travel is rest
	else if (choice == adjList.Length) {
		m_health += 10;
		if (m_health > 100) {
			m_health = 100;
		}
	}
	// option after rest is resupply
	else if (choice > adjList.Length) {
		if (m_gold < 10) {
			m_node.Call("draw_event", "You're poor!", "You do not have enough money for this.", new string[]{ "Okay." });
			m_state = State.WAIT_CHOICE_EVENT;
			m_eventId = -1; // signifies that we're just waiting for a response. any response.
			return;
		}
		m_food += 10;
		m_gold -= 10;
		if (m_food > 100) {
			m_food = 100;
		}
	}
}


// TravelEdge(int)
// Travels to the desired SeaNode.
// A random event will be fired.
// =============================================
// choice:    User's choice.
private void TravelEdge(int destination) {
	m_nodeId = destination;
	m_food -= 10;
	
	m_nodes[destination].Visit();
	m_map[m_nodes[destination].Row, m_nodes[destination].Col] = '@';

	// using System // needed for random
	int fired = RandomEvent();
	
	m_food += m_events[fired].GetDeltaFood();
	m_gold += m_events[fired].GetDeltaGold();
	m_health += m_events[fired].GetDeltaHealth();
	GameOver();
	
	m_node.Call("draw_event", m_events[fired].GetTitle(), m_events[fired].GetDescription(), m_events[fired].GetChoiceDescriptions());

	m_state = State.WAIT_CHOICE_EVENT;
	m_eventId = fired;


	return;
}

// EventChoiceHandler(int)
// Chooses an available option for responding to an event (possibly firing another event in the process).
// Invalid events will be ignored.
// =============================================
// choice:    User's choice.
private void EventChoiceHandler(int choice) {
	if (m_eventId == -1) {
		m_state = State.WAIT_CHOICE_NODE;
		PrintMap();
		return;		
	}
	if (choice < 0 || choice > m_events[m_eventId].NumChoices()) {
		return;
	}

	int dest = (m_events[m_eventId].GetChoiceDestinations())[choice];

	if (dest == -1) {
		m_state = State.WAIT_CHOICE_NODE;
		PrintMap();
	}
	else {
		m_food += m_events[m_eventId].GetDeltaFood();
		m_gold += m_events[m_eventId].GetDeltaGold();
		m_health += m_events[m_eventId].GetDeltaHealth();
		GameOver();
		
		m_node.Call("draw_event", m_events[m_eventId].GetTitle(), m_events[m_eventId].GetDescription(), m_events[m_eventId].GetChoiceDescriptions());

		m_state = State.WAIT_CHOICE_EVENT;
		m_eventId = dest;
	}
}


// PrintMap(void)
// Constructs and draws a view of the map based on the current node the user is present in, marking the node with *.
private void PrintMap() {	
	int centerRow = m_nodes[m_nodeId].Row;
	int centerCol = m_nodes[m_nodeId].Col;
	
	int top = centerRow - (gridHeight - 1) / 2;
	int bottom = centerRow + (gridHeight - 1) / 2;
	if (top < 0) {
		top = 0;
		bottom = gridHeight - 1;
	}
	else if (bottom >= mapHeight) {
		top = mapHeight - gridHeight - 2;
		bottom = mapHeight - 1;
	}

	int left = centerCol - (gridWidth - 1) / 2;
	int right = centerCol + (gridWidth - 1) / 2;
	if (left < 0) {
		left = 0;
		right = gridWidth - 1;
	}
	else if (right >= mapWidth) {
		left = mapWidth - gridWidth - 2;
		right = gridWidth - 1;
	}
	
	// Construct the map as a string
	StringBuilder sb = new StringBuilder();
	GD.Print(m_nodes[m_nodeId].Row, m_nodes[m_nodeId].Col);
	char prev = m_map[m_nodes[m_nodeId].Row, m_nodes[m_nodeId].Col];
	m_map[m_nodes[m_nodeId].Row, m_nodes[m_nodeId].Col] = '*';
	for (int row = top; row <= bottom; ++row) {
		for (int col = left; col <= right; ++col) {
			sb.Append(m_map[row, col]);
		}
		sb.Append("\n");
	}
	m_map[m_nodes[m_nodeId].Row, m_nodes[m_nodeId].Col] = prev;
	
	int[] adjList = m_nodes[m_nodeId].GetAdjacencyList();	
	m_node.Call("draw_map", sb.ToString(), (object) adjList, gridHeight, gridWidth);

	// DrawMap(sb.ToString(), adjList, numRows, numCols);
}

public int[] NodeCoordinates(int nodeIndex) {
	int[] ret = new int[2];
	ret[0] = m_nodes[nodeIndex].Row;
	ret[1] = m_nodes[nodeIndex].Col;
	return ret;
}

public string NodeName(int nodeIndex) {
	return m_nodes[nodeIndex].GetName();
}

private void GameOver() {
	if (m_health <= 0) {
		m_node.Call("draw_event", "Death!", "Due to continuous and multiple injuries suffered by your body without proper care, your body has stopped cooperating with you.", new string[]{ "Okay." });
		// exit
	}
	if (m_food <= 0) {
		m_node.Call("draw_event", "Death!", "Long voyages with no resupply has left the ship with no food or any edible object to speak of. You and your crew suffer a slow, painful death by starvation.", new string[]{ "Okay." });
		//DrawEvent("Death!", "Long voyages with no resupply has left the ship with no food or any edible object to speak of. You and your crew suffer a slow, painful death by starvation.", { "Okay." });
		// exit
	}
}

// GAME RESOURCES 
private char[,] m_map = null;
private SeaNode[] m_nodes = null;

private State m_state;
private Event[] m_events = null;
private float[] m_eventWeights = null;
private float m_totalWeight = 0.0f;
private Random m_random = null;

// USER RESOURCES
private int m_nodeId = 13;
private int m_eventId = -1;

private int m_health = 100;
private int m_gold = 100;
private int m_food = 100;

private Godot.Object m_node;
}
