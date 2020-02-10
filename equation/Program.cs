using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

namespace paren
{
    class Node
    {
        public string prepend;
        public string text;
        public Node parent;
        public List<Node> childNodes = new List<Node>();

        public void Print(StringBuilder sb, int level)
        {
            sb.Append("\r\n");
            sb.Append(new string(' ', level * 4));
            sb.Append(prepend);
            sb.Append('(');
            foreach (Node n in childNodes)
            {
                n.Print(sb, level + 1);
            }
            sb.Append(text);
            sb.Append(')');
        }
    }
    class Program
    {
        static string textstr = @"deriv_r = (2 * (-tx + x_dst - (pow2(ux) * (1 - cosr) + cosr) * x_src - (ux * uy * (1 - cosr) + sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * y_src - (ux * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) - uy * sinr) * z_src) * (-(-sinr + pow2(ux) * sinr) * x_src - (sqrt(1 - pow2(ux) - pow2(uy)) * cosr + ux * uy * sinr) * y_src - (-uy * cosr + ux * sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * z_src) + 2 * (-ty - (ux * uy * (1 - cosr) - sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * x_src + y_dst - (pow2(uy) * (1 - cosr) + cosr) * y_src - (uy * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) + ux * sinr) * z_src) * (-(-sqrt(1 - pow2(ux) - pow2(uy)) * cosr + ux * uy * sinr) * x_src - (-sinr + pow2(uy) * sinr) * y_src - (ux * cosr + uy * sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * z_src) + 2 * (-tz - (ux * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) + uy * sinr) * x_src - (uy * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) - ux * sinr) * y_src + z_dst - ((1 - pow2(ux) - pow2(uy)) * (1 - cosr) + cosr) * z_src) * (-(uy * cosr + ux * sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * x_src - (-ux * cosr + uy * sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * y_src - (-sinr + (1 - pow2(ux) - pow2(uy)) * sinr) * z_src));
                deriv_u.X = (2 * (-(-((pow2(ux) * (1 - cosr)) / sqrt(1 - pow2(ux) - pow2(uy))) + sqrt(1 - pow2(ux) - pow2(uy)) * 
                    (1 - cosr)) * x_src - (-((ux * uy * (1 - cosr)) / sqrt(1 - pow2(ux) - pow2(uy))) - sinr) * y_src + 2 * ux * (1 - cosr) * z_src) * 
                    (-tz - (ux * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) + uy * sinr) * x_src - (uy * sqrt(1 - pow2(ux) - pow2(uy)) * 
                    (1 - cosr) - ux * sinr) * y_src + z_dst - ((1 - pow2(ux) - pow2(uy)) * (1 - cosr) + cosr) * z_src) + 2 * (-(uy * (1 - cosr) + (ux * sinr) / 
                    sqrt(1 - pow2(ux) - pow2(uy))) * x_src - (-((ux * uy * (1 - cosr)) / sqrt(1 - pow2(ux) - pow2(uy))) + sinr) * z_src) * 
                    (-ty - (ux * uy * (1 - cosr) - sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * x_src + y_dst - (pow2(uy) * (1 - cosr) + cosr) * y_src - 
                    (uy * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) + ux * sinr) * z_src) + 2 * (-2 * ux * (1 - cosr) * x_src - (uy * (1 - cosr) - (ux * sinr) / 
                    sqrt(1 - pow2(ux) - pow2(uy))) * y_src - (-((pow2(ux) * (1 - cosr)) / sqrt(1 - pow2(ux) - pow2(uy))) + sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr)) * z_src) * 
                    (-tx + x_dst - (pow2(ux) * (1 - cosr) + cosr) * x_src - (ux * uy * (1 - cosr) + sqrt(1 - pow2(ux) - pow2(uy)) * sinr) * y_src - (ux * sqrt(1 - pow2(ux) - pow2(uy)) * (1 - cosr) - uy * sinr) * z_src));";

        static void Main(string[] args)
        {

            Node topNode = new Node();
            topNode.parent = null;
            Node curNode = topNode;
            int charpos = 0;

            textstr = textstr.Replace("\r\n", "");
            while (textstr.Contains("  "))
                textstr = textstr.Replace("  ", " ");
            while (true)
            {
                int lastpos = charpos + 1;
                charpos = textstr.IndexOfAny(new char[] { '(', ')' }, lastpos);
                if (charpos < 0)
                    break;

                if (textstr[charpos] == '(')
                {
                    Node newNode = new Node();
                    newNode.parent = curNode;
                    curNode.childNodes.Add(newNode);
                    newNode.prepend = textstr.Substring(lastpos, charpos - lastpos);
                    curNode = newNode;
                }
                else
                {
                    curNode.text = textstr.Substring(lastpos, charpos - lastpos);
                    curNode = curNode.parent;
                }
            }

            StringBuilder sb = new StringBuilder();
            topNode.Print(sb, 0);
            Console.Write(sb.ToString());
        }
    }
}
