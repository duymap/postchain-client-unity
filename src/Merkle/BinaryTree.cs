using System.Collections.Generic;
using System.Linq;
using System;

namespace Chromia.PostchainClient.GTV
{
    enum HashPrefix
    {
        Node = 0,
        Leaf = 1,
        NodeArray = 7,
        NodeDict = 8
    }

    public abstract class BinaryTreeElement
    {
        private PathElement PathElem = null;

        public bool IsPath()
        {
            return this.PathElem != null;
        }

        public bool IsPathLeaf()
        {
            if (this.PathElem == null)
            {
                return false;
            }

            return this.PathElem is PathLeafElement;
        }

        public void SetPathElement(PathElement pathElem)
        {
            this.PathElem = pathElem;
        }

        public abstract int GetPrefixByte();
    }

    public class Node: BinaryTreeElement
    {
        public BinaryTreeElement Left { get; }
        public BinaryTreeElement Right { get; }

        public Node(BinaryTreeElement left, BinaryTreeElement right)
        {
            this.Left = left;
            this.Right = right;
        }

        public override int GetPrefixByte()
        {
            return (int) HashPrefix.Node;
        }
    }

    public class SubTreeRootNode<T>: Node
    {
        private T Content;

        public SubTreeRootNode(BinaryTreeElement left, BinaryTreeElement right, T content, PathElement pathElem = null) : base(left, right)
        {
            this.Content = content;
            SetPathElement(pathElem);
        }
    }

    public class Leaf<T>: BinaryTreeElement
    {
        private dynamic Content;

        public Leaf(dynamic content, PathElement pathElem = null)
        {
            this.Content = content;

            if (pathElem != null)
            {
                if (pathElem is PathLeafElement)
                {
                    SetPathElement(pathElem);
                }
                else
                {
                    throw new System.Exception("The path and object structure does not match! We are at a leaf, but the path expects a sub structure.");
                }
            }
        }

        public override int GetPrefixByte()
        {
            return (int) HashPrefix.Leaf;
        }
    }

    public class EmptyLeaf: BinaryTreeElement
    {
        public EmptyLeaf(){}

        public override int GetPrefixByte()
        {
            return (int) HashPrefix.Leaf;
        }
    }

    public class BinaryTree
    {
        BinaryTreeElement Root;

        public BinaryTree(BinaryTreeElement root)
        {
            this.Root = root;
        }

        public int MaxLevel()
        {
            return this.MaxLevelInternal(this.Root);
        }

        private int MaxLevelInternal(BinaryTreeElement node)
        {
            if (node is EmptyLeaf)
            {
                return 0;
            }
            else if (node is Leaf)
            {
                return 1;
            }
            else if (node is Node)
            {
                var castedNode = (Node) node;
                return Math.Max(this.MaxLevelInternal(castedNode.Left), this.MaxLevelInternal(castedNode.Right)) + 1;
            }
            else
            {
                throw new System.Exception("What is this type? " + node.GetType().ToString());
            }
        }
    }

    public class ArrayHeadNode<T>: SubTreeRootNode<T>
    {
        private int Size;
        public ArrayHeadNode(BinaryTreeElement left, BinaryTreeElement right, T content, int size, PathElement pathElem = null) : base(left, right, content, pathElem)
        {
            this.Size = size;
        }

        public override int GetPrefixByte()
        {
            return (int) HashPrefix.NodeArray;
        }
    }

    public class DictHeadNode<T>: SubTreeRootNode<T>
    {
        private int Size;

        public DictHeadNode(BinaryTreeElement left, BinaryTreeElement right, T content, int size, PathElement pathElem = null) : base(left, right, content, pathElem)
        {
            this.Size = size;
        }

        public override int GetPrefixByte()
        {
            return (int) HashPrefix.NodeDict;
        }
    }
}