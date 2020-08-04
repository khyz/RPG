﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

namespace RPG.Dialogue
{
    [CreateAssetMenu(menuName = ("RPG/Dialogue"))]
    public class Dialogue : ScriptableObject
    {
        [SerializeField]
        bool IsPlayerFirstSpeaker = true;
        [SerializeField]
        List<DialogueNode> nodes = new List<DialogueNode>();
        Dictionary<string, DialogueNode> nodeLookup = new Dictionary<string, DialogueNode>();
        Dictionary<string, List<string>> parentLookup = new Dictionary<string, List<string>>();

        private void OnValidate() {
#if UNITY_EDITOR
            if (nodes.Count <= 0)
            {
                CreateNode(null);
            }
#endif
            BuildNodeLookup();
            BuildParentLookup();
            SubscribeForChanges();
            ValidateSameSpeaker();
        }

        private void ValidateSameSpeaker()
        {
            foreach (DialogueNode node in GetAllNodes())
            {
                List<string> childrenToRemove = new List<string>();
                foreach (string childName in node.GetChildren())
                {
                    if (node.IsPlayerNextSpeaker() != IsPlayerSpeaking(childName))
                    {
                        childrenToRemove.Add(childName);
                    }
                }
                foreach (string childToRemove in childrenToRemove)
                {
                    node.RemoveChild(childToRemove);
                }
            }
        }

        private void SubscribeForChanges()
        {
            foreach (DialogueNode node in GetAllNodes())
            {
                node.OnChange -= OnValidate;
                node.OnChange += OnValidate;
            }
        }

        private void BuildParentLookup()
        {
            parentLookup.Clear();
            foreach (DialogueNode node in GetAllNodes())
            {
                foreach (string childName in node.GetChildren())
                {
                    if (!parentLookup.ContainsKey(childName))
                    {
                        parentLookup[childName] = new List<string>();
                    }
                    parentLookup[childName].Add(node.name);
                }
            }
        }

        private void BuildNodeLookup()
        {
            nodeLookup.Clear();
            foreach (DialogueNode node in GetAllNodes())
            {
                nodeLookup[node.name] = node;
            }
        }

        public IEnumerable<DialogueNode> GetAllNodes()
        {
            return nodes;
        }

        public IEnumerable<DialogueNode> GetChildren(DialogueNode node)
        {
            if (node == null)
            {
                foreach (DialogueNode potentialRoot in nodes)
                {
                    if (!parentLookup.ContainsKey(potentialRoot.name))
                    {
                        yield return potentialRoot;
                    }
                }
                
            }
            else
            {
                foreach (string childID in node.GetChildren())
                {
                    if (nodeLookup.ContainsKey(childID))
                    {
                        yield return nodeLookup[childID];
                    }
                }
            }
        }

        public bool IsPlayerNext(DialogueNode node)
        {
            if (node == null)
            {
                return IsPlayerFirstSpeaker;
            }

            return node.IsPlayerNextSpeaker();
        }

        public bool IsPlayerSpeaking(string nodeName)
        {
            if (!parentLookup.ContainsKey(nodeName))
            {
                return IsPlayerFirstSpeaker;
            }
            return nodeLookup[parentLookup[nodeName][0]].IsPlayerNextSpeaker();
        }

#if UNITY_EDITOR
        public DialogueNode CreateNode(DialogueNode parent)
        {   
            DialogueNode newNode = CreateInstance<DialogueNode>();
            Undo.RegisterCreatedObjectUndo(newNode, "");
            newNode.name = System.Guid.NewGuid().ToString();
            if (parent != null)
            {
                Vector2 childOffset = new Vector2(200, 0);
                newNode.SetPosition(parent.GetRect().position + childOffset);
                parent.AddChild(newNode.name);
            }
            nodes.Add(newNode);
            AssetDatabase.AddObjectToAsset(newNode, this);
            newNode.SetNextSpeaker(!parent.IsPlayerNextSpeaker());
            OnValidate();
            return newNode;
        }

        public void DeleteNode(DialogueNode deletingNode)
        {
            nodes.Remove(deletingNode);
            OnValidate();
            CleanDanglingChildren(deletingNode.name);
            Undo.DestroyObjectImmediate(deletingNode);
        }

        private void CleanDanglingChildren(string IDToRemove)
        {
            foreach (DialogueNode node in GetAllNodes())
            {
                node.RemoveChild(IDToRemove);
            }
        }
#endif
    }
}