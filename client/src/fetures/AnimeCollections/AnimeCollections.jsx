import React, { useEffect } from "react";
import { useDispatch, useSelector } from "react-redux";
import { useParams } from "react-router-dom";
import Layout from "../../layouts/Layout/Layout";
import Animes from "../../shared/Animes/component/Animes";
import AreaCategories from "../../shared/AreaCategories/component/AreaCategories";
import AnimeRanks from "../../shared/Ranks/AnimeRanks";
import { animeCollections } from "../../reduxs/doSomethings";

export default function AnimeCollections(){
    const { meta } = useParams();

    const collections = useSelector(state => state.collections.data);
    const indexes = collections.filter(x => x.key === meta);

    const animeCollects = useSelector(state => state.animeCollections.data);
    const dispatch = useDispatch();

    useEffect(() => {
        dispatch(animeCollections(meta))
    }, [dispatch, meta]);

    return(
        <Layout 
            title={ indexes.length > 0 ? `AnimeAB - ${indexes[0].title}` : "" } 
            descript={ indexes.length > 0 ? `AnimeAB - ${indexes[0].title}` : "" }>
            <div className="main-pad">
                <div className="anis-content">
                    <div className="anis-cate">
                        <AreaCategories isIcon={true}></AreaCategories>
                    </div>
                    <div className="anis-content-wrapper">
                        <div className="content">
                            <Animes animes={animeCollects} flewBig={true} title={indexes.length > 0 ? indexes[0].title : ""}></Animes>
                        </div>
                        <AnimeRanks></AnimeRanks>
                    </div>
                </div>
            </div>
        </Layout>
    )
}