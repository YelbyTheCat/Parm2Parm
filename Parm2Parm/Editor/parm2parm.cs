using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ExpressionParameters = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters;
using ExpressionParameter = VRC.SDK3.Avatars.ScriptableObjects.VRCExpressionParameters.Parameter;
using UnityEditor.Animations;
using System.Linq;

/**
 * Appends parameters to Controll From Parameter Menu and vise versa
 * @author yelby
 */

public class parm2parm : EditorWindow
{
    ExpressionParameters parameters = null;
    AnimatorController controller = null;
    List<ParameterSystem> parameterSystems = new List<ParameterSystem>();
    List<ParameterSystem> controllerSystem = new List<ParameterSystem>();

    Vector2 scrollPose;
    //Toolbar
    int toolBar = 0;
    string[] toolBarSections = { "Avatar To Controller", "Controller To Avatar" };

    [MenuItem("Yelby/Param2Param")]
    public static void ShowWindow()
    {
        GetWindow<parm2parm>("Param2Param");
    }

    private void OnGUI()
    {
        GUILayout.Label("Version: 1.0");

        parameters = EditorGUILayout.ObjectField("Avatar Parameters: ", parameters, typeof(ExpressionParameters), true) as ExpressionParameters;
        controller = EditorGUILayout.ObjectField("Controller: ", controller, typeof(AnimatorController), true) as AnimatorController;

        if (parameters == null || controller == null)
            return;

        toolBar = GUILayout.Toolbar(toolBar, toolBarSections);

        switch (toolBar)
        {
            case 0: // Avatar to Controller
                fillList(parameters);
                displayList(parameterSystems);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("All OFF"))
                    setAll(parameterSystems, false);
                if (GUILayout.Button("All ON"))
                    setAll(parameterSystems, true);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Transfer"))
                {
                    AnimatorControllerParameter[] tempList = avatarToController(parameters, controller);
                    if (tempList != null)
                        controller.parameters = tempList;
                }

                break;
            case 1: // Controller to Avatar
                fillList(controller);
                displayList(controllerSystem);

                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("All OFF"))
                    setAll(controllerSystem, false);
                if (GUILayout.Button("All ON"))
                    setAll(controllerSystem, true);
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("Transfer"))
                {
                    ExpressionParameter[] tempList = ControllerToAvatar(parameters, controller);
                    if (tempList != null)
                    {
                        ExpressionParameters tempParameter = new ExpressionParameters();
                        tempParameter.parameters = tempList;
                        int max = 128;
                        int cost = tempParameter.CalcTotalCost();
                        if (cost <= max)
                            parameters.parameters = tempList;
                        else
                        {
                            string message = "Out of bits " + cost + "/" + max;
                            Debug.LogError(message);
                            EditorUtility.DisplayDialog("Out of Bits", message, "OK");
                            return;
                        }
                    }
                }
                break;
        }
    }

    // Methods ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~

    public void fillList(ExpressionParameters parameters)
    {
        ExpressionParameter[] paramList = parameters.parameters;
        int paramSize = paramList.Length;

        for (int i = 0; i < paramSize; i++)
        {
            var current = paramList[i];
            bool add = true;
            for(int j = 0; j < parameterSystems.Count; j++)
            {
                if (parameterSystems[j].name == current.name)
                {
                    add = false;
                    break;
                }
            }

            if(add)
                parameterSystems.Add(new ParameterSystem(true, current.name, current.valueType.ToString().ToLower()));
        }
    }

    public void fillList(AnimatorController parameters)
    {
        AnimatorControllerParameter[] paramList = parameters.parameters;
        int paramSize = paramList.Length;

        for (int i = 0; i < paramSize; i++)
        {
            var current = paramList[i];
            bool add = true;
            for (int j = 0; j < controllerSystem.Count; j++)
            {
                if (controllerSystem[j].name == current.name)
                {
                    add = false;
                    break;
                }
            }

            if (add)
                controllerSystem.Add(new ParameterSystem(true, current.name, current.type.ToString().ToLower()));
        }
    }

    public void displayList(List<ParameterSystem> system)
    {
        scrollPose = EditorGUILayout.BeginScrollView(scrollPose);
        for (int i = 0; i < system.Count; i++)
        {
            var current = system[i];
            GUILayout.BeginHorizontal();
            current.add = EditorGUILayout.Toggle("Transfer: ", current.add);
            GUILayout.Label(current.name);
            GUILayout.Label(current.type);
            GUILayout.EndHorizontal();
            system[i] = current;
        }
        GUILayout.EndScrollView();
    }

    public void setAll(List<ParameterSystem> system, bool set)
    {
        for (int i = 0; i < system.Count; i++)
        {
            var current = system[i];
            current.add = set;
        }
    }

    public void printList(List<ParameterSystem> system)
    {
        for (int i = 0; i < system.Count; i++)
        {
            var current = system[i];
            Debug.Log("Add:" + current.add + " Name:" + current.name + " Type:" + current.type);
        }
    }

    public AnimatorControllerParameter[] avatarToController(ExpressionParameters parameters, AnimatorController controller)
    {
        List<ExpressionParameter> avatarList = parameters.parameters.ToList();
        int avatarSize = avatarList.Count;

        List<AnimatorControllerParameter> controllerList = controller.parameters.ToList();
        int controllerSize = controllerList.Count;

        for (int i = 0; i < avatarSize; i++)
        {
            var currentAvatar = avatarList[i];
            string currentAvatarType = currentAvatar.valueType.ToString().ToLower();
            bool add = true;

            // Contains
            for (int j = 0; j < controllerSize; j++)
            {
                var currentController = controllerList[j];
                if (currentAvatar.name == currentController.name) // If name exists
                {
                    string currentControllerType = currentController.type.ToString().ToLower();
                    if (currentAvatarType == currentControllerType) // If the same type
                    {
                        add = false;
                        break;
                    }
                    else // Same name, different type
                    {
                        string message = "Type Mismatch " + currentAvatar.name;
                        Debug.LogError(message);
                        EditorUtility.DisplayDialog("Type Mismatch", message, "OK");
                        return null;
                    }
                }
            }

            if(add)
            {
                bool skip = false;
                for (int j = 0; j < parameterSystems.Count; j++)
                {
                    var current = parameterSystems[j];
                    if (currentAvatar.name == current.name)
                    {
                        if (!current.add)
                        {
                            skip = true;
                            break;
                        }
                    }
                }

                if (skip)
                    continue;

                AnimatorControllerParameter temp = new AnimatorControllerParameter();
                temp.name = currentAvatar.name;
                switch (currentAvatarType)
                {
                    case "bool":
                        temp.type = AnimatorControllerParameterType.Bool;
                        break;
                    case "int":
                        temp.type = AnimatorControllerParameterType.Int;
                        break;
                    case "float":
                        temp.type = AnimatorControllerParameterType.Float;
                        break;
                    case "trigger":
                        temp.type = AnimatorControllerParameterType.Trigger;
                        break;
                }
                controllerList.Add(temp);
            }
        }

        return controllerList.ToArray();
    }

    public ExpressionParameter[] ControllerToAvatar(ExpressionParameters parameters, AnimatorController controller)
    {
        List<ExpressionParameter> avatarList = parameters.parameters.ToList();
        int avatarSize = avatarList.Count;

        List<AnimatorControllerParameter> controllerList = controller.parameters.ToList();
        int controllerSize = controllerList.Count;

        for (int i = 0; i < controllerSize; i++)
        {
            
            var currentController = controllerList[i];
            string currentControllerType = currentController.type.ToString().ToLower();
            bool add = true;

            // Contains
            for (int j = 0; j < avatarSize; j++)
            {
                var currentAvatar = avatarList[j];
                if (currentAvatar.name == currentController.name) // If name exists
                {
                    string currentAvatarType = currentAvatar.valueType.ToString().ToLower();
                    if (currentAvatarType == currentControllerType) // If the same type
                    {
                        add = false;
                        break;
                    }
                    else // Same name, different type
                    {
                        string message = "Type Mismatch " + currentController.name;
                        Debug.LogError(message);
                        EditorUtility.DisplayDialog("Type Mismatch", message, "OK");
                        return null;
                    }
                }
            }

            if (add)
            {
                bool skip = false;
                for (int j = 0; j < controllerSystem.Count; j++)
                {
                    var current = controllerSystem[j];
                    if (currentController.name == current.name)
                    {
                        if (!current.add)
                        {
                            skip = true;
                            break;
                        }
                    }
                }

                if (skip)
                    continue;

                ExpressionParameter temp = new ExpressionParameter();
                temp.name = currentController.name;
                switch (currentControllerType)
                {
                    case "bool":
                        temp.valueType = ExpressionParameters.ValueType.Bool;
                        break;
                    case "int":
                        temp.valueType = ExpressionParameters.ValueType.Int;
                        break;
                    case "float":
                        temp.valueType = ExpressionParameters.ValueType.Float;
                        break;
                }
                avatarList.Add(temp);
            }
        }

        return avatarList.ToArray();
    }
}

public class ParameterSystem
{
    public bool add;
    public string name;
    public string type;

    public ParameterSystem(bool add, string name, string type)
    {
        this.add = add;
        this.name = name;
        this.type = type;
    }
}