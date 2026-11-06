namespace Bold.Tests;

/// <summary>Hand-written XMI files in the forms tools write: Rose's XMI 1.0 of UML 1.3, and XMI 1.2 of UML 1.4.</summary>
public static class XmiSamples
{
    /// <summary>A shop: a package, a class with a Rose tagged value, a stereotype, an attribute with a multiplicity and an
    /// initial value, an operation with parameters, a generalization, an unnamed association, an association class, a
    /// constraint, and a state machine and an extension the importer skips.</summary>
    public const string Rose13 = """
        <?xml version="1.0" encoding="windows-1252"?>
        <XMI xmi.version="1.0">
          <XMI.header>
            <XMI.documentation><XMI.exporter>Unisys.JCR.2</XMI.exporter></XMI.documentation>
            <XMI.metamodel xmi.name="UML" xmi.version="1.3"/>
          </XMI.header>
          <XMI.content>
            <Model_Management.Model xmi.id="G.0" xmi.uuid="UUID-MODEL">
              <Foundation.Core.ModelElement.name>Shop</Foundation.Core.ModelElement.name>
              <Foundation.Core.Namespace.ownedElement>
                <Model_Management.Package xmi.id="P1">
                  <Foundation.Core.ModelElement.name>Sales</Foundation.Core.ModelElement.name>
                  <Foundation.Core.Namespace.ownedElement>
                    <Foundation.Core.Class xmi.id="C1" xmi.uuid="UUID-CUSTOMER">
                      <Foundation.Core.ModelElement.name>Customer</Foundation.Core.ModelElement.name>
                      <Foundation.Core.ModelElement.visibility xmi.value="public"/>
                      <Foundation.Core.GeneralizableElement.isAbstract xmi.value="false"/>
                      <Foundation.Core.ModelElement.stereotype>
                        <Foundation.Extension_Mechanisms.Stereotype xmi.idref="S1"/>
                      </Foundation.Core.ModelElement.stereotype>
                      <Foundation.Core.ModelElement.taggedValue>
                        <Foundation.Extension_Mechanisms.TaggedValue xmi.id="T1">
                          <Foundation.Extension_Mechanisms.TaggedValue.tag>RationalRose$Bold:TableName</Foundation.Extension_Mechanisms.TaggedValue.tag>
                          <Foundation.Extension_Mechanisms.TaggedValue.value>CUSTOMERS</Foundation.Extension_Mechanisms.TaggedValue.value>
                        </Foundation.Extension_Mechanisms.TaggedValue>
                      </Foundation.Core.ModelElement.taggedValue>
                      <XMI.extension xmi.extender="Rational Rose"><RoseProperty name="x"/></XMI.extension>
                      <Foundation.Core.Classifier.feature>
                        <Foundation.Core.Attribute xmi.id="A1">
                          <Foundation.Core.ModelElement.name>name</Foundation.Core.ModelElement.name>
                          <Foundation.Core.ModelElement.visibility xmi.value="package"/>
                          <Foundation.Core.StructuralFeature.multiplicity>
                            <Foundation.Data_Types.Multiplicity>
                              <Foundation.Data_Types.Multiplicity.range>
                                <Foundation.Data_Types.MultiplicityRange>
                                  <Foundation.Data_Types.MultiplicityRange.lower>1</Foundation.Data_Types.MultiplicityRange.lower>
                                  <Foundation.Data_Types.MultiplicityRange.upper>1</Foundation.Data_Types.MultiplicityRange.upper>
                                </Foundation.Data_Types.MultiplicityRange>
                              </Foundation.Data_Types.Multiplicity.range>
                            </Foundation.Data_Types.Multiplicity>
                          </Foundation.Core.StructuralFeature.multiplicity>
                          <Foundation.Core.Attribute.initialValue>
                            <Foundation.Data_Types.Expression>
                              <Foundation.Data_Types.Expression.language/>
                              <Foundation.Data_Types.Expression.body>'anonymous'</Foundation.Data_Types.Expression.body>
                            </Foundation.Data_Types.Expression>
                          </Foundation.Core.Attribute.initialValue>
                          <Foundation.Core.StructuralFeature.type>
                            <Foundation.Core.DataType xmi.idref="D1"/>
                          </Foundation.Core.StructuralFeature.type>
                        </Foundation.Core.Attribute>
                        <Foundation.Core.Operation xmi.id="O1">
                          <Foundation.Core.ModelElement.name>Rename</Foundation.Core.ModelElement.name>
                          <Foundation.Core.Feature.ownerScope xmi.value="classifier"/>
                          <Foundation.Core.BehavioralFeature.parameter>
                            <Foundation.Core.Parameter xmi.id="PA1">
                              <Foundation.Core.ModelElement.name>NewName</Foundation.Core.ModelElement.name>
                              <Foundation.Core.Parameter.kind xmi.value="in"/>
                              <Foundation.Core.Parameter.type><Foundation.Core.DataType xmi.idref="D1"/></Foundation.Core.Parameter.type>
                            </Foundation.Core.Parameter>
                            <Foundation.Core.Parameter xmi.id="PA2">
                              <Foundation.Core.ModelElement.name>return</Foundation.Core.ModelElement.name>
                              <Foundation.Core.Parameter.kind xmi.value="return"/>
                              <Foundation.Core.Parameter.type><Foundation.Core.DataType xmi.idref="D2"/></Foundation.Core.Parameter.type>
                            </Foundation.Core.Parameter>
                          </Foundation.Core.BehavioralFeature.parameter>
                        </Foundation.Core.Operation>
                      </Foundation.Core.Classifier.feature>
                    </Foundation.Core.Class>
                    <Foundation.Core.Class xmi.id="C2">
                      <Foundation.Core.ModelElement.name>Order</Foundation.Core.ModelElement.name>
                      <Foundation.Core.GeneralizableElement.specialization>
                        <Foundation.Core.Generalization xmi.idref="G1"/>
                      </Foundation.Core.GeneralizableElement.specialization>
                    </Foundation.Core.Class>
                    <Foundation.Core.Class xmi.id="C3">
                      <Foundation.Core.ModelElement.name>BigOrder</Foundation.Core.ModelElement.name>
                      <Foundation.Core.GeneralizableElement.generalization>
                        <Foundation.Core.Generalization xmi.idref="G1"/>
                      </Foundation.Core.GeneralizableElement.generalization>
                    </Foundation.Core.Class>
                    <Foundation.Core.Generalization xmi.id="G1">
                      <Foundation.Core.Generalization.child><Foundation.Core.Class xmi.idref="C3"/></Foundation.Core.Generalization.child>
                      <Foundation.Core.Generalization.parent><Foundation.Core.Class xmi.idref="C2"/></Foundation.Core.Generalization.parent>
                    </Foundation.Core.Generalization>
                    <Foundation.Core.Association xmi.id="AS1">
                      <Foundation.Core.Association.connection>
                        <Foundation.Core.AssociationEnd xmi.id="E1">
                          <Foundation.Core.ModelElement.name>customer</Foundation.Core.ModelElement.name>
                          <Foundation.Core.AssociationEnd.multiplicity>
                            <Foundation.Data_Types.Multiplicity>
                              <Foundation.Data_Types.Multiplicity.range>
                                <Foundation.Data_Types.MultiplicityRange>
                                  <Foundation.Data_Types.MultiplicityRange.lower>1</Foundation.Data_Types.MultiplicityRange.lower>
                                  <Foundation.Data_Types.MultiplicityRange.upper>1</Foundation.Data_Types.MultiplicityRange.upper>
                                </Foundation.Data_Types.MultiplicityRange>
                              </Foundation.Data_Types.Multiplicity.range>
                            </Foundation.Data_Types.Multiplicity>
                          </Foundation.Core.AssociationEnd.multiplicity>
                          <Foundation.Core.AssociationEnd.type><Foundation.Core.Class xmi.idref="C1"/></Foundation.Core.AssociationEnd.type>
                        </Foundation.Core.AssociationEnd>
                        <Foundation.Core.AssociationEnd xmi.id="E2">
                          <Foundation.Core.ModelElement.name>orders</Foundation.Core.ModelElement.name>
                          <Foundation.Core.AssociationEnd.ordering xmi.value="ordered"/>
                          <Foundation.Core.AssociationEnd.aggregation xmi.value="composite"/>
                          <Foundation.Core.AssociationEnd.multiplicity>
                            <Foundation.Data_Types.Multiplicity>
                              <Foundation.Data_Types.Multiplicity.range>
                                <Foundation.Data_Types.MultiplicityRange>
                                  <Foundation.Data_Types.MultiplicityRange.lower>0</Foundation.Data_Types.MultiplicityRange.lower>
                                  <Foundation.Data_Types.MultiplicityRange.upper>-1</Foundation.Data_Types.MultiplicityRange.upper>
                                </Foundation.Data_Types.MultiplicityRange>
                              </Foundation.Data_Types.Multiplicity.range>
                            </Foundation.Data_Types.Multiplicity>
                          </Foundation.Core.AssociationEnd.multiplicity>
                          <Foundation.Core.AssociationEnd.type><Foundation.Core.Class xmi.idref="C2"/></Foundation.Core.AssociationEnd.type>
                        </Foundation.Core.AssociationEnd>
                      </Foundation.Core.Association.connection>
                    </Foundation.Core.Association>
                    <Foundation.Core.AssociationClass xmi.id="AC1">
                      <Foundation.Core.ModelElement.name>Membership</Foundation.Core.ModelElement.name>
                      <Foundation.Core.Classifier.feature>
                        <Foundation.Core.Attribute xmi.id="A2">
                          <Foundation.Core.ModelElement.name>since</Foundation.Core.ModelElement.name>
                          <Foundation.Core.StructuralFeature.type><Foundation.Core.DataType xmi.idref="D2"/></Foundation.Core.StructuralFeature.type>
                        </Foundation.Core.Attribute>
                      </Foundation.Core.Classifier.feature>
                      <Foundation.Core.Association.connection>
                        <Foundation.Core.AssociationEnd xmi.id="E3">
                          <Foundation.Core.ModelElement.name>members</Foundation.Core.ModelElement.name>
                          <Foundation.Core.AssociationEnd.multiplicity>
                            <Foundation.Data_Types.Multiplicity>
                              <Foundation.Data_Types.Multiplicity.range>
                                <Foundation.Data_Types.MultiplicityRange>
                                  <Foundation.Data_Types.MultiplicityRange.lower>0</Foundation.Data_Types.MultiplicityRange.lower>
                                  <Foundation.Data_Types.MultiplicityRange.upper>-1</Foundation.Data_Types.MultiplicityRange.upper>
                                </Foundation.Data_Types.MultiplicityRange>
                              </Foundation.Data_Types.Multiplicity.range>
                            </Foundation.Data_Types.Multiplicity>
                          </Foundation.Core.AssociationEnd.multiplicity>
                          <Foundation.Core.AssociationEnd.type><Foundation.Core.Class xmi.idref="C1"/></Foundation.Core.AssociationEnd.type>
                        </Foundation.Core.AssociationEnd>
                        <Foundation.Core.AssociationEnd xmi.id="E4">
                          <Foundation.Core.ModelElement.name>clubs</Foundation.Core.ModelElement.name>
                          <Foundation.Core.AssociationEnd.multiplicity>
                            <Foundation.Data_Types.Multiplicity>
                              <Foundation.Data_Types.Multiplicity.range>
                                <Foundation.Data_Types.MultiplicityRange>
                                  <Foundation.Data_Types.MultiplicityRange.lower>0</Foundation.Data_Types.MultiplicityRange.lower>
                                  <Foundation.Data_Types.MultiplicityRange.upper>-1</Foundation.Data_Types.MultiplicityRange.upper>
                                </Foundation.Data_Types.MultiplicityRange>
                              </Foundation.Data_Types.Multiplicity.range>
                            </Foundation.Data_Types.Multiplicity>
                          </Foundation.Core.AssociationEnd.multiplicity>
                          <Foundation.Core.AssociationEnd.type><Foundation.Core.Class xmi.idref="C4"/></Foundation.Core.AssociationEnd.type>
                        </Foundation.Core.AssociationEnd>
                      </Foundation.Core.Association.connection>
                    </Foundation.Core.AssociationClass>
                    <Foundation.Core.Class xmi.id="C4">
                      <Foundation.Core.ModelElement.name>Club</Foundation.Core.ModelElement.name>
                    </Foundation.Core.Class>
                    <Foundation.Core.Constraint xmi.id="K1">
                      <Foundation.Core.ModelElement.name>hasName</Foundation.Core.ModelElement.name>
                      <Foundation.Core.Constraint.body>
                        <Foundation.Data_Types.BooleanExpression>
                          <Foundation.Data_Types.Expression.language>OCL</Foundation.Data_Types.Expression.language>
                          <Foundation.Data_Types.Expression.body>name &lt;&gt; ''</Foundation.Data_Types.Expression.body>
                        </Foundation.Data_Types.BooleanExpression>
                      </Foundation.Core.Constraint.body>
                      <Foundation.Core.Constraint.constrainedElement><Foundation.Core.Class xmi.idref="C1"/></Foundation.Core.Constraint.constrainedElement>
                    </Foundation.Core.Constraint>
                    <Behavioral_Elements.State_Machines.StateMachine xmi.id="SM1">
                      <Foundation.Core.ModelElement.name>Lifecycle</Foundation.Core.ModelElement.name>
                    </Behavioral_Elements.State_Machines.StateMachine>
                  </Foundation.Core.Namespace.ownedElement>
                </Model_Management.Package>
                <Foundation.Extension_Mechanisms.Stereotype xmi.id="S1">
                  <Foundation.Core.ModelElement.name>entity</Foundation.Core.ModelElement.name>
                  <Foundation.Extension_Mechanisms.Stereotype.baseClass>Class</Foundation.Extension_Mechanisms.Stereotype.baseClass>
                </Foundation.Extension_Mechanisms.Stereotype>
                <Foundation.Core.DataType xmi.id="D1">
                  <Foundation.Core.ModelElement.name>String</Foundation.Core.ModelElement.name>
                </Foundation.Core.DataType>
                <Foundation.Core.DataType xmi.id="D2">
                  <Foundation.Core.ModelElement.name>Integer</Foundation.Core.ModelElement.name>
                </Foundation.Core.DataType>
              </Foundation.Core.Namespace.ownedElement>
            </Model_Management.Model>
          </XMI.content>
        </XMI>
        """;

    /// <summary>A library in the form of UML 1.4 tools: members as XML attributes, participant for an end's class,
    /// tagged values as data values of tag definitions, and references by id in attributes.</summary>
    public const string Uml14 = """
        <?xml version="1.0" encoding="UTF-8"?>
        <XMI xmi.version="1.2" xmlns:UML="org.omg.xmi.namespace.UML">
          <XMI.header><XMI.metamodel xmi.name="UML" xmi.version="1.4"/></XMI.header>
          <XMI.content>
            <UML:Model xmi.id="m1" name="Library" isSpecification="false" isRoot="false" isLeaf="false" isAbstract="false">
              <UML:Namespace.ownedElement>
                <UML:Stereotype xmi.id="s1" name="entity">
                  <UML:Stereotype.baseClass>Class</UML:Stereotype.baseClass>
                </UML:Stereotype>
                <UML:Class xmi.id="c1" name="Book" visibility="public" isAbstract="true" stereotype="s1">
                  <UML:ModelElement.taggedValue>
                    <UML:TaggedValue xmi.id="tv1">
                      <UML:TaggedValue.dataValue>BOOKS</UML:TaggedValue.dataValue>
                      <UML:TaggedValue.type><UML:TagDefinition xmi.idref="td1"/></UML:TaggedValue.type>
                    </UML:TaggedValue>
                  </UML:ModelElement.taggedValue>
                  <UML:Classifier.feature>
                    <UML:Attribute xmi.id="a1" name="title" visibility="private" ownerScope="instance">
                      <UML:StructuralFeature.multiplicity>
                        <UML:Multiplicity xmi.id="mu1">
                          <UML:Multiplicity.range><UML:MultiplicityRange xmi.id="r1" lower="1" upper="1"/></UML:Multiplicity.range>
                        </UML:Multiplicity>
                      </UML:StructuralFeature.multiplicity>
                      <UML:Attribute.initialValue><UML:Expression xmi.id="x1" language="java" body="'untitled'"/></UML:Attribute.initialValue>
                      <UML:StructuralFeature.type><UML:DataType xmi.idref="dt1"/></UML:StructuralFeature.type>
                    </UML:Attribute>
                  </UML:Classifier.feature>
                </UML:Class>
                <UML:Class xmi.id="c2" name="Author"/>
                <UML:Class xmi.id="c3" name="Novel" generalization="g1"/>
                <UML:Generalization xmi.id="g1" child="c3" parent="c1"/>
                <UML:Association xmi.id="as1" name="Writes">
                  <UML:Association.connection>
                    <UML:AssociationEnd xmi.id="e1" name="authors" isNavigable="true" ordering="ordered" aggregation="none">
                      <UML:AssociationEnd.multiplicity>
                        <UML:Multiplicity xmi.id="mu2">
                          <UML:Multiplicity.range><UML:MultiplicityRange xmi.id="r2" lower="1" upper="-1"/></UML:Multiplicity.range>
                        </UML:Multiplicity>
                      </UML:AssociationEnd.multiplicity>
                      <UML:AssociationEnd.participant><UML:Class xmi.idref="c2"/></UML:AssociationEnd.participant>
                    </UML:AssociationEnd>
                    <UML:AssociationEnd xmi.id="e2" name="books" isNavigable="true" participant="c1">
                      <UML:AssociationEnd.multiplicity>
                        <UML:Multiplicity xmi.id="mu3">
                          <UML:Multiplicity.range><UML:MultiplicityRange xmi.id="r3" lower="0" upper="-1"/></UML:Multiplicity.range>
                        </UML:Multiplicity>
                      </UML:AssociationEnd.multiplicity>
                    </UML:AssociationEnd>
                  </UML:Association.connection>
                </UML:Association>
                <UML:DataType xmi.id="dt1" name="String"/>
                <UML:TagDefinition xmi.id="td1" name="Bold.TableName" tagType="String"/>
              </UML:Namespace.ownedElement>
            </UML:Model>
          </XMI.content>
        </XMI>
        """;

    /// <summary>Writes a sample to a new file in a test folder.</summary>
    public static string Write(string content, string name)
    {
        string dir = Path.Combine(Path.GetTempPath(), "BoldXmiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        string path = Path.Combine(dir, name);
        File.WriteAllText(path, content, name.Contains("Rose") ? System.Text.Encoding.Latin1 : System.Text.Encoding.UTF8);
        return path;
    }

    /// <summary>A new file name in a test folder.</summary>
    public static string NewFile(string name) => Write("", name);

    public static TMoldModel LoadFixture(string file)
    {
        var lines = new TStringList();
        lines.LoadFromFile(FixturePaths.Model(file));
        return TMoldBLDRW.StringsToModel(lines);
    }
}

/// <summary>XMI 2 files in the forms of Enterprise Architect and of Eclipse's UML (Papyrus).</summary>
public static class Xmi2Samples
{
    /// <summary>A hotel as Enterprise Architect writes it: member ends as elements, its types and tags in its extension.</summary>
    public const string EnterpriseArchitect = """
        <?xml version="1.0" encoding="windows-1252"?>
        <xmi:XMI xmi:version="2.1" xmlns:uml="http://schema.omg.org/spec/UML/2.1" xmlns:xmi="http://schema.omg.org/spec/XMI/2.1">
          <xmi:Documentation exporter="Enterprise Architect" exporterVersion="6.5"/>
          <uml:Model xmi:type="uml:Model" name="EA_Model" visibility="public">
            <packagedElement xmi:type="uml:Package" xmi:id="EAPK_1" name="Hotel" visibility="public">
              <packagedElement xmi:type="uml:Class" xmi:id="EAID_GUEST" name="Guest" visibility="public">
                <ownedAttribute xmi:type="uml:Property" xmi:id="EAID_A1" name="name" visibility="private" isStatic="false" isReadOnly="false" isDerived="false" isOrdered="false" isUnique="true">
                  <lowerValue xmi:type="uml:LiteralInteger" xmi:id="EAID_LI1" value="1"/>
                  <upperValue xmi:type="uml:LiteralUnlimitedNatural" xmi:id="EAID_LI2" value="1"/>
                  <type xmi:idref="EAJava_String"/>
                </ownedAttribute>
                <ownedOperation xmi:id="EAID_O1" name="Checkout" visibility="public" concurrency="sequential">
                  <ownedParameter xmi:id="EAID_P1" name="early" direction="in" type="EAJava_boolean"/>
                  <ownedParameter xmi:id="EAID_RT1" name="return" direction="return" type="EAJava_int"/>
                </ownedOperation>
              </packagedElement>
              <packagedElement xmi:type="uml:Class" xmi:id="EAID_ROOM" name="Room" visibility="public"/>
              <packagedElement xmi:type="uml:Class" xmi:id="EAID_SUITE" name="Suite" visibility="public">
                <generalization xmi:type="uml:Generalization" xmi:id="EAID_G1" general="EAID_ROOM"/>
              </packagedElement>
              <packagedElement xmi:type="uml:Class" xmi:id="EAID_BED" name="Bed" visibility="public"/>
              <packagedElement xmi:type="uml:Association" xmi:id="EAID_AS1" name="Stay" visibility="public">
                <memberEnd xmi:idref="EAID_dst1"/>
                <memberEnd xmi:idref="EAID_src1"/>
                <ownedEnd xmi:type="uml:Property" xmi:id="EAID_src1" name="guests" association="EAID_AS1" aggregation="none">
                  <type xmi:idref="EAID_GUEST"/>
                  <lowerValue xmi:type="uml:LiteralInteger" value="0"/>
                  <upperValue xmi:type="uml:LiteralUnlimitedNatural" value="-1"/>
                </ownedEnd>
                <ownedEnd xmi:type="uml:Property" xmi:id="EAID_dst1" name="room" association="EAID_AS1" aggregation="none">
                  <type xmi:idref="EAID_ROOM"/>
                  <lowerValue xmi:type="uml:LiteralInteger" value="0"/>
                  <upperValue xmi:type="uml:LiteralUnlimitedNatural" value="1"/>
                </ownedEnd>
              </packagedElement>
              <packagedElement xmi:type="uml:Association" xmi:id="EAID_AS2" name="Furnishing" visibility="public">
                <memberEnd xmi:idref="EAID_beds"/>
                <memberEnd xmi:idref="EAID_room2"/>
                <ownedEnd xmi:type="uml:Property" xmi:id="EAID_beds" name="beds" association="EAID_AS2" aggregation="composite" isOrdered="true">
                  <type xmi:idref="EAID_BED"/>
                  <lowerValue xmi:type="uml:LiteralInteger" value="1"/>
                  <upperValue xmi:type="uml:LiteralUnlimitedNatural" value="*"/>
                </ownedEnd>
                <ownedEnd xmi:type="uml:Property" xmi:id="EAID_room2" name="room" association="EAID_AS2" aggregation="none">
                  <type xmi:idref="EAID_ROOM"/>
                </ownedEnd>
              </packagedElement>
            </packagedElement>
          </uml:Model>
          <xmi:Extension extender="Enterprise Architect" extenderID="6.5">
            <elements>
              <element xmi:idref="EAID_GUEST" xmi:type="uml:Class" name="Guest" scope="public">
                <properties isSpecification="false" sType="Class" nType="0" scope="public" stereotype="person"/>
                <tags><tag xmi:id="EAID_T1" name="Bold.TableName" value="GUESTS" modelElement="EAID_GUEST"/></tags>
              </element>
            </elements>
            <primitivetypes>
              <packagedElement xmi:type="uml:Package" xmi:id="EAPrimitiveTypesPackage" name="EA_PrimitiveTypes_Package">
                <packagedElement xmi:type="uml:Package" xmi:id="EAJavaTypesPackage" name="EA_Java_Types_Package">
                  <packagedElement xmi:type="uml:PrimitiveType" xmi:id="EAJava_String" name="String"/>
                  <packagedElement xmi:type="uml:PrimitiveType" xmi:id="EAJava_boolean" name="Boolean"/>
                  <packagedElement xmi:type="uml:PrimitiveType" xmi:id="EAJava_int" name="Integer"/>
                </packagedElement>
              </packagedElement>
            </primitivetypes>
          </xmi:Extension>
        </xmi:XMI>
        """;

    /// <summary>A zoo as Eclipse's UML writes it: implicit member types, library types by href, a navigable end owned
    /// by its class, and an applied stereotype with a value.</summary>
    public const string Papyrus = """
        <?xml version="1.0" encoding="UTF-8"?>
        <xmi:XMI xmi:version="20131001" xmlns:xmi="http://www.omg.org/spec/XMI/20131001" xmlns:uml="http://www.eclipse.org/uml2/5.0.0/UML" xmlns:BoldProfile="http:///schemas/BoldProfile/1">
          <uml:Model xmi:id="_m" name="Zoo">
            <packagedElement xmi:type="uml:Class" xmi:id="_animal" name="Animal" isAbstract="true">
              <ownedRule xmi:id="_r1" name="hasLegs" constrainedElement="_animal">
                <specification xmi:type="uml:OpaqueExpression" xmi:id="_r1s"><language>OCL</language><body>legs &gt;= 0</body></specification>
              </ownedRule>
              <ownedAttribute xmi:id="_legs" name="legs">
                <type xmi:type="uml:PrimitiveType" href="pathmap://UML_LIBRARIES/UMLPrimitiveTypes.library.uml#Integer"/>
                <defaultValue xmi:type="uml:LiteralInteger" xmi:id="_d" value="4"/>
              </ownedAttribute>
              <ownedAttribute xmi:id="_keeper" name="keeper" type="_keeperClass" association="_cares">
                <lowerValue xmi:type="uml:LiteralInteger" xmi:id="_l1"/>
                <upperValue xmi:type="uml:LiteralUnlimitedNatural" xmi:id="_u1" value="1"/>
              </ownedAttribute>
            </packagedElement>
            <packagedElement xmi:type="uml:Class" xmi:id="_keeperClass" name="Keeper"/>
            <packagedElement xmi:type="uml:Association" xmi:id="_cares" name="Cares" memberEnd="_keeper _animals">
              <ownedEnd xmi:id="_animals" name="animals" type="_animal" association="_cares">
                <lowerValue xmi:type="uml:LiteralInteger" xmi:id="_l2"/>
                <upperValue xmi:type="uml:LiteralUnlimitedNatural" xmi:id="_u2" value="*"/>
              </ownedEnd>
            </packagedElement>
          </uml:Model>
          <BoldProfile:Persistent xmi:id="_s1" base_Class="_animal" TableName="ANIMALS"/>
        </xmi:XMI>
        """;
}
