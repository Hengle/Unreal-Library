﻿using System.Windows.Forms;

namespace UELib.Core
{
	public partial class UStruct
	{
		protected override void InitNodes( TreeNode node )
		{
			_ParentNode = AddSectionNode( node, typeof(UStruct).Name );
			if( GetType() == typeof(UStruct) )
			{
				var sFlagsNode = AddTextNode( _ParentNode, "Struct Flags:" + UnrealMethods.FlagToString( StructFlags ) );
				sFlagsNode.ToolTipText = UnrealMethods.FlagsListToString( UnrealMethods.FlagsToList( typeof(Flags.StructFlags), StructFlags ) );
			}

			if( ScriptBuffer != null )
			{
				var objN = new ObjectNode( ScriptBuffer ) {Text = ScriptBuffer.Name};
				node.Nodes.Add( objN );
			}

			#if DEBUG	
				AddTextNode( _ParentNode, "Script Index:" + ScriptText ); 
				AddTextNode( _ParentNode, "Children Index:" + Children );
				AddTextNode( _ParentNode, "CppText Index:" + CppText );
				AddTextNode( _ParentNode, "FriendlyName Index:" + FriendlyNameIndex );
				AddTextNode( _ParentNode, "Script Size:" + ScriptSize );
			#endif
			base.InitNodes( _ParentNode );
		}

		protected override void AddChildren( TreeNode node )
		{
			AddObjectListNode( node, "Constants", _ChildConstants );
			AddObjectListNode( node, "Enumerations", _ChildEnums );
			AddObjectListNode( node, "Structures", _ChildStructs );

			// Not if the upper class is a function; UFunction adds locals and parameters instead
			if( GetType() != typeof(UFunction) )
			{
				AddObjectListNode( node, "Properties", _ChildProperties );
			}
		}

		protected override void PostAddChildren( TreeNode node )
		{
			if( _Properties != null && _Properties.Count > 0 )
			{
				var defNode = new ObjectListNode{Text = "Default Values"};
				node.Nodes.Add( defNode );
				foreach( var def in _Properties )
				{	
					var objN = new DefaultObjectNode( def ){Text = def.Tag.Name};
					defNode.Nodes.Add( objN );
				}
			}
		}
	}
}
