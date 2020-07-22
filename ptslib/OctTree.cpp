// dllmain.cpp : Defines the entry point for the DLL application.
#include "pch.h"
#include <cmath>
#include <vector>
#include <set>
#include <map>
#include <memory>
#include <algorithm>
#include "Pt.h"
#include <gmtl/gmtl.h>
#include <gmtl/Vec.h>
#include <gmtl/Matrix.h>
#include <vector>
#include "OctGrid.h"
#include "OctTree.h"
#include "OctTreeLoc.h"

using namespace gmtl;


////////////////////////////////////////////////////////////////////////////////////////////////////

template <typename ValueType, int M, int ML> typename OctTree<ValueType, M, ML>::Node& OctTree<ValueType, M, ML>::
AddPoint(const PointType& pt)
{
	OctTreeLoc loc = OctTreeLoc::GetTileAtLoc(pt.loc, sMaxLevel, m_bounds);
	Node& node = m_topNode.AddPoint(*this, loc, pt);
	m_ptCnt++;
	return node;
}



////////////////////////////////////////////////////////////////////////////////////////////////////

template <typename ValueType, int M, int ML> void OctNode<ValueType, M, ML>::
GetLeafNodes(std::vector<const OctNode*>& leafNodes, bool getEmptyNodes) const
{
	if (m_children.size() > 0 && m_pts.size() > 0)
		__debugbreak();

	if (m_children.size() == 0 &&
		(getEmptyNodes || m_pts.size() > 0))
	{
		leafNodes.push_back(this);
	}

	for (const std::shared_ptr<OctNode>& child : m_children)
	{
		child->GetLeafNodes(leafNodes, getEmptyNodes);
	}
}

////////////////////////////////////////////////////////////////////////////////////////////////////

template <typename ValueType, int M, int ML> OctNode<ValueType, M, ML>& OctNode<ValueType, M, ML>::
AddPoint(const OctTree<ValueType, M, ML>& tree, const OctTreeLoc& loc, const PointType& pt)
{
	m_csNode.Acquire();
	if (m_isLeaf)
	{
		m_pts.push_back(std::make_pair(loc, pt));
		if (m_loc.m_level < OctTree<ValueType, M, ML>::sMaxLevel &&
			loc.m_level > m_loc.m_level)
		{
			m_children.reserve(8);
			for (size_t idx = 0; idx < 8; ++idx)
			{
				m_children.push_back(std::make_shared<OctNode>(m_loc.GetChild(idx)));
			}
			for (auto& ipt : m_pts)
			{
				const OctTreeLoc& iloc = ipt.first;
				size_t childIdx = m_loc.IndexOfChild(iloc);
				m_children[childIdx]->AddPoint(tree, ipt.first, ipt.second);
			}

			m_pts = std::vector<std::pair<OctTreeLoc, PointType>>();
			m_isLeaf = false;
		}
		m_csNode.Release();
	}
	else
	{
		size_t childIdx = m_loc.IndexOfChild(loc);
		m_csNode.Release();
		m_children[childIdx]->AddPoint(tree, loc, pt);
	}
	return *this;
}

////////////////////////////////////////////////////////////////////////////////////////////////////
// Instantiations
////////////////////////////////////////////////////////////////////////////////////////////////////

template class WX_EXPORT_SHARED OctTree<size_t, 64, 8>;
template class WX_EXPORT_SHARED OctTree<size_t, 64, 9>;
template class WX_EXPORT_SHARED OctTree<wxfloat32, 1, 9>;

