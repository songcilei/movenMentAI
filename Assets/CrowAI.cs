using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.PlayerLoop;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector3 = UnityEngine.Vector3;

public class CrowAI : MonoBehaviour
{

    public Transform target;
    public float speed = 1;
    public float animRandomTime = 2f;
    private Animation anim;

    public Vector3 startVelocity; 
    
    
    public Vector3 sumForce = Vector3.zero;//合力

    public Vector3 separationForce = Vector3.zero;//分离的力
    public Vector3 alignmentForce = Vector3.zero;//列队的力
    public Vector3 cohesionForce = Vector3.zero;//聚集的力
    
    
    public float separationDistance = 3;//距离这个鸟有3米以内的鸟类，跟其他鸟类进行分离
    public float alignmentDistance = 1;
    public List<GameObject> separationNeighbors = new List<GameObject>();//存储附近的鸟
    public List<GameObject> alignmentNeighbors = new List<GameObject>();
    public float separationWeight = 1;//分离的力所占的比重
    public float alignmentWeight = 1;//列队的力所占的比重
    public float cohesionWeight = 1;//聚集的里所占的比重
    public float m = 1;//鸟的质量
    public Vector3 velocity = Vector3.zero;

    public float checkInteterval = 0.2f;

    public float ColliderSize = 0.4f;
    private bool drawGizmo = false;
    private float dist;
    private Vector3 direction;

    private void Start()
    {
        InvokeRepeating("CalForce",0,checkInteterval);
        anim = GetComponentInChildren<Animation>();
        //每隔0-2随机数等待
        Invoke("PlayAnim",Random.Range(0,animRandomTime));
        
    }

    void PlayAnim()
    {
        //anim.Play();
    }

    void CalForce()
    {
        sumForce = Vector3.zero;
        separationForce = Vector3.zero;
        alignmentForce = Vector3.zero;
        cohesionForce = Vector3.zero;
        
        separationNeighbors.Clear();
        alignmentNeighbors.Clear();
        drawGizmo = false;
        direction = Vector3.zero;
        dist = 0;
        Collider[] colliders = Physics.OverlapSphere(transform.position, separationDistance);
        foreach (var c in colliders)
        {
            if (c != null && c.gameObject != this.gameObject)
            {
                if (c.gameObject.layer == LayerMask.NameToLayer("IgnoreObj")) continue ;
                if (c.name == this.transform.name) continue;

                separationNeighbors.Add(c.gameObject);
            }


        
        }
        
        //计算分离的力
        foreach (var neighbor in separationNeighbors)
        {
            if (transform.position == neighbor.transform.position)
            {
                transform.position = new Vector3(transform.position.x+0.01f,transform.position.y+0.01f,transform.position.z+0.01f);
            }
            //需要远离的方向
            Vector3 dir = transform.position - neighbor.transform.position;


            //距离越小，施加的力越大
            separationForce += dir.normalized / dir.magnitude;
        }
        
        if (separationNeighbors.Count>0)
        {
            separationForce *= separationWeight;
            //Debug.Log(separationForce);
            sumForce += separationForce;
        }
        
        //计算队列的力
        colliders = Physics.OverlapSphere(transform.position, alignmentDistance);
        foreach (Collider c in colliders)
        {
            if (c!=null && c.gameObject != this.gameObject)
            {
                if (c.gameObject.layer == LayerMask.NameToLayer("IgnoreObj")) continue ;

                alignmentNeighbors.Add(c.gameObject);
            }
        }

        Vector3 avgDir = Vector3.zero;
        foreach (GameObject n in alignmentNeighbors) {
            //Debug.Log("nForward:"+n.transform.forward);
            avgDir += n.transform.forward;
        }

        if (alignmentNeighbors.Count > 0) {
            avgDir /= alignmentNeighbors.Count;
            alignmentForce = avgDir - transform.forward;
//            Debug.Log("avgDir:"+avgDir+":::::::forward:"+transform.forward);

            alignmentForce *= alignmentWeight;   
            sumForce += alignmentForce;
        }
        

        //聚集的力
        //if (alignmentNeighbors.Count <= 0) return;
        
        Vector3 center = Vector3.zero;
        foreach (GameObject n in alignmentNeighbors)
        {
            center += n.transform.position;
        }
        
        center /= alignmentNeighbors.Count;
        Vector3 dirToCenter = center - transform.position;
        cohesionForce += dirToCenter;
        cohesionForce *= cohesionWeight;
        sumForce += alignmentForce;

        

        
        //向某个物体飞去
        Vector3 targetDir = target.transform.position - transform.position;
        if (targetDir==Vector3.zero)
        {
            targetDir += new Vector3(0, 0, 0.01f);
        }

        //temp 方向减去 当前方向
        sumForce +=(targetDir.normalized - transform.forward)*speed;
    }


    private void Update()
    {
        
        //牛顿第二定律,计算加速度
        Vector3 a = sumForce / m;
        //v = at 速度计算公式
        velocity += a * Time.deltaTime;
//      Physics.ComputePenetration()
        InterObjInvok();
        transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(velocity), Time.deltaTime*10);
        transform.Translate(transform.forward*velocity.magnitude* Time.deltaTime,Space.World);

    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position,separationDistance);
        Gizmos.color = Color.green;
        Gizmos.DrawLine(transform.position,transform.position+transform.forward*3);
        if (drawGizmo)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position,transform.position+direction*dist);

            Gizmos.DrawCube(transform.position,new Vector3(ColliderSize,ColliderSize,ColliderSize));
        }

    }


    void InterObjInvok()
    {
        //当碰见其他交互物体时
        Collider[] colliders = Physics.OverlapSphere(transform.position,ColliderSize);
        foreach (var c in colliders)
        {
            if (c.gameObject.layer == LayerMask.NameToLayer("InterObj"))//检测是否是交互物体
            {
                drawGizmo = true;
                Collider interObjCollider = c.GetComponent<Collider>();
                Collider selfObjCollider = this.GetComponent<Collider>();
    
                bool overlap = Physics.ComputePenetration(
                    selfObjCollider,transform.position,transform.rotation,
                    interObjCollider,c.transform.position,c.transform.rotation,
                    out direction,out dist);
                if (overlap)
                {
                    direction = new Vector3(direction.x,0,direction.z);
                    //transform.Translate(direction * dist);
                    Debug.DrawRay(transform.position,direction*dist,Color.red);
                    transform.position += direction * dist;
                }


            }
        }
        
    }
}
