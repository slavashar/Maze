using System;
using System.Linq.Expressions;
using Maze.Nodes;
using Xunit;

namespace Maze.Facts
{
    public class NodeFacts
    {
        [Fact]
        public void get_an_empty_node()
        {
            var node = NodeFactory.Empty;

            node.Kind.ShouldEqual(NodeKind.Empty);
        }

        [Fact]
        public void create_a_text_node()
        {
            var node = NodeFactory.Text("source");

            node.Kind.ShouldEqual(NodeKind.Text);
            node.Value.ShouldEqual("source");
        }

        [Fact]
        public void try_to_create_a_text_node_without_text()
        {
            Assert.Throws<ArgumentNullException>(() => NodeFactory.Text(null));
        }

        [Fact]
        public void create_an_unary_node()
        {
            var brackets = new UnaryToken();

            var item = NodeFactory.Text("item");
            var node = item.Then(brackets);

            node[UnaryToken.Parent].ShouldBe(item);
        }

        [Fact]
        public void create_a_typed_unary_node()
        {
            var brackets = new UnaryToken<TestClass>(x => x.Parent);

            var item = NodeFactory.Text("item");
            var node = item.Then(new TestClass(), brackets);

            node[UnaryToken.Parent].ShouldBe(item);
        }

        [Fact]
        public void create_an_item_node()
        {
            var constant = new ItemToken();

            var value = NodeFactory.Text("value");
            var node = NodeFactory.ItemNode(constant, value);
        }

        [Fact]
        public void create_a_typed_item_node()
        {
            var constant = new ItemToken<TestClass>(x => x.Value);

            var value = NodeFactory.Text("value");
            var node = NodeFactory.ItemNode(new TestClass(), constant, value);
        }

        [Fact]
        public void crate_an_unary_item_node()
        {
            var member = new UnaryItemToken();

            var item = NodeFactory.Text("item");
            var value = NodeFactory.Text("value");

            var node = item.Then(member, value);
        }

        [Fact]
        public void crate_a_typed_unary_item_node()
        {
            var member = new UnaryItemToken<TestClass>(x => x.Parent, x => x.Value);

            var item = NodeFactory.Text("item");
            var value = NodeFactory.Text("value");
            var node = item.Then(new TestClass(), member, value);
        }

        [Fact]
        public void create_a_binary_node()
        {
            var equal = new BinaryToken();

            var left = NodeFactory.Text("left");
            var right = NodeFactory.Text("right");
            var node = NodeFactory.BinaryNode(equal, left, right);
        }

        [Fact]
        public void create_a_child_token_node()
        {
            var equal = new BinaryToken<TestClass>(x => x.Parent, x => x.Value);

            var left = NodeFactory.Text("left");
            var right = NodeFactory.Text("right");
            var node = NodeFactory.BinaryNode(new TestClass(), equal, left, right);
        }

        [Fact]
        public void crete_multi_item_node()
        {
            var left = NodeFactory.Text("left");
            var right = NodeFactory.Text("right");

            var node = NodeFactory.MultipleItems(left, right);
        }

        [Fact]
        public void get_a_node_from_expression()
        {
            var member = new UnaryItemToken<TestClass>(x => x.Parent, x => x.Value);

            var item = NodeFactory.Text("item");
            var value = NodeFactory.Text("value");
            var node = item.Then(new TestClass(), member, value);

            node.Get(x => x.Value).ShouldBe(value);
            node[UnaryItemToken.Item].ShouldBe(value);
        }

        [Fact]
        public void replace_a_token()
        {
            var equal = new BinaryToken();
            var notequal = new BinaryToken();

            var left = NodeFactory.Text("left");
            var right = NodeFactory.Text("right");
            var node = NodeFactory.BinaryNode(equal, left, right);

            var result = node.WithToken(notequal);

            result.Token.ShouldBe(notequal);
            result[BinaryToken.Left].ShouldBe(left);
            result[BinaryToken.Right].ShouldBe(right);
        }

        [Fact]
        public void replace_a_token_in_element_node()
        {
            var equal = new BinaryToken();
            var notequal = new BinaryToken();

            var left = NodeFactory.Text("left");
            var right = NodeFactory.Text("right");
            var node = NodeFactory.BinaryNode(new TestClass(), equal, left, right);

            var result = node.WithToken(notequal);

            result.Token.ShouldBe(notequal);
            result[BinaryToken.Left].ShouldBe(left);
            result[BinaryToken.Right].ShouldBe(right);
        }

        [Fact]
        public void get_parent_node()
        {
            var member = new UnaryItemToken();

            var item = NodeFactory.Text("item");
            var value = NodeFactory.Text("value");
            var node = item.Then(member, value);

            var result = node.GetParent();

            result.ShouldBe(item);
        }

        [Fact]
        public void try_to_get_parent_from_multiple_sources()
        {
            var member = new UnaryItemToken();

            var item = NodeFactory.MultipleItems(NodeFactory.Text("item1"), NodeFactory.Text("item2"));
            var value = NodeFactory.Text("value");
            var node = item.Then(member, value);
            
            Assert.Throws<InvalidOperationException>(() => node.GetParent());
        }

        [Fact]
        public void get_parents_from_multiple_sources()
        {
            var member = new UnaryItemToken();

            var item1 = NodeFactory.Text("item1");
            var item2 = NodeFactory.Text("item2");
            var value = NodeFactory.Text("value");
            var node = NodeFactory.MultipleItems(item1, item2).Then(member, value);

            var result = node.GetParents();

            result.ShouldEqual(item1, item2);
        }

        [Fact]
        public void get_parents_from_binary_node()
        {
            var equal = new BinaryToken();

            var left = NodeFactory.Text("left");
            var right = NodeFactory.Text("right");
            var node = NodeFactory.BinaryNode(equal, left, right);

            var result = node.GetParents();

            result.ShouldEqual(left, right);
        }

        [Fact]
        public void find_by_token_in_token_node()
        {
            var constant = new ItemToken();
            var brackets = new UnaryToken();

            var node = NodeFactory.ItemNode(constant, NodeFactory.Text("item"));
            var item = node.Then(brackets);

            var find = item.Find(constant);

            find.Nodes.ShouldEqual(node);
        }

        [Fact]
        public void find_by_token_in_element_node()
        {
            var constant = new ItemToken();
            var brackets = new UnaryToken();

            var node = NodeFactory.ItemNode(new TestClass(), constant, NodeFactory.Text("item"));
            var item = node.Then(brackets);

            var find = item.Find(constant);

            find.Nodes.ShouldEqual(node);
        }

        [Fact]
        public void change_found_token_node()
        {
            var constant = new ItemToken();
            var brackets = new UnaryToken();

            var node = NodeFactory.ItemNode(constant, NodeFactory.Text("item")).Then(brackets);

            var find = node.Find(constant);

            var result = find.Change(x => x[ItemToken.Item]);

            var resultnode = result.ShouldBeType<TokenNode<UnaryToken>>();
            resultnode.Token.ShouldBe(brackets);

            resultnode[UnaryToken.Parent].ShouldBeType<TextNode>().Value.ShouldEqual("item");
        }

        [Fact]
        public void find_element_node()
        {
            var constant = new ItemToken<TestClass>(x => x.Value);
            var brackets = new UnaryToken();

            var node = NodeFactory.ItemNode(new TestClass(), constant, NodeFactory.Text("item"));
            var item = node.Then(brackets);

            var find = item.Find<TestClass>(x => x.Element is TestClass);

            find.Nodes.ShouldEqual(node);
        }

        [Fact]
        public void change_found_element_node()
        {
            var constant = new ItemToken<TestClass>(x => x.Value);
            var brackets = new UnaryToken();

            var node = NodeFactory.ItemNode(new TestClass(), constant, NodeFactory.Text("item"));
            var item = node.Then(brackets);

            var find = item.Find<TestClass>(x => x.Element is TestClass);

            var result = find.Change(x => x[ItemToken.Item]);

            var resultnode = result.ShouldBeType<TokenNode<UnaryToken>>();
            resultnode.Token.ShouldBe(brackets);

            resultnode[UnaryToken.Parent].ShouldBeType<TextNode>().Value.ShouldEqual("item");
        }

        [Fact]
        public void change_found_token_node_with_parent()
        {
            var member = new UnaryItemToken();
            var bind = new UnaryItemToken();
            var brackets = new UnaryToken();

            var node = NodeFactory.Text("source")
                .Then(member, NodeFactory.Text("item"))
                .Then(brackets);

            var find = node.Find(member);

            var result = find.Change(x => x.WithToken(bind));

            var resultnode = result.ShouldBeType<TokenNode<UnaryToken>>();
            resultnode.Token.ShouldBe(brackets);

            resultnode[UnaryToken.Parent].ShouldBeType<TokenNode<UnaryItemToken>>().Token.ShouldBe(bind);
        }

        [Fact]
        // regression test
        public void replace_convert_node()
        {
            Expression<Func<char?, bool>> map = ch => ch == 'A';

            var graph = ExpressionNodeBuilder.Parse(((BinaryExpression)map.Body).Right);

            var find = graph.Find<UnaryExpression>(node => node.Element.NodeType == ExpressionType.Convert);

            find.Nodes.Count.ShouldEqual(2);

            var result = find.Change(node => node.Get(e => e.Operand));

            result.Stringify().ShouldEqual("\'A\'");
        }

        private class TestClass
        {
            public object Parent { get; set; }

            public object Value { get; set; }
        }
    }
}
