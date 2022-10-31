import React, { Component } from 'react';
import { SurveyDisplay } from './SurveyDisplay';
import { SurveyApiClient } from './surveyApiClient';
import { Loading } from '../../controls/loading';
import { ErrorControl } from '../../controls/error-control';
import { QuestionApiClient } from '../question/questionApiClient';
import { QuestionDisplay } from '../question/QuestionDisplay';
import { QuestionOptionApiClient } from "../question-option/questionOptionApiClient";
import { Answer } from "../answer/Answer";
import { QuestionOption } from "../question-option/QuestionOption";
import { ResponseApiClient } from "../response/responseApiClient";
import { AnswerApiClient } from "../answer/answerApiClient";

interface QuestionAnswer {
    question: QuestionDisplay;
    answer: Answer;
    options: QuestionOption[] | null;
    loadingOptions: boolean;
    loadingOptionsError: boolean;
}

interface SurveyCompleteState {
    data: SurveyDisplay | null;
    questionsAndAnswers: QuestionAnswer[] | null;
    loading: boolean;
    error: boolean;
    loadingQuestion: boolean;
    questionError: boolean;
    responseError: boolean;
    message?: string;
    submissionComplete: boolean;
}

export class SurveyComplete extends Component<any, SurveyCompleteState> {

    private api: SurveyApiClient = new SurveyApiClient();
    private questionApi: QuestionApiClient = new QuestionApiClient();
    private responseApi: ResponseApiClient = new ResponseApiClient();
    private answerApi: AnswerApiClient = new AnswerApiClient();
    private questionOptionApi: QuestionOptionApiClient = new QuestionOptionApiClient();
    private defaultDisplayOrder = 1000; // a suitably big number 

    constructor(props: any) {
        super(props);
        this.state = {
            data: null,
            questionsAndAnswers: null,
            loadingQuestion: true,
            questionError: false,
            responseError: false,
            loading: true,
            error: false,
            submissionComplete: false
        };

        this.handleAnswerChange = this.handleAnswerChange.bind(this);
        this.handleOptionChange = this.handleOptionChange.bind(this);
        this.handleSubmit = this.handleSubmit.bind(this);
    }

    componentDidMount() {
        const { id } = this.props.match.params;
        this.getItemDetails(id);
        this.getQuestionDetails(id);
        console.log(this.props.history.location);
    }

    handleSubmit(event: React.FormEvent<HTMLFormElement>) {
        if (!event.currentTarget.checkValidity()) {
            return;
        }

        if (this.state.data && this.state.questionsAndAnswers) {
            this.responseApi.insert(this.state.data.id).then(responseData => {
                var responseId = responseData.parsedBody;
                if (this.state.questionsAndAnswers && responseId) {
                    this.state.questionsAndAnswers.forEach(qa => this.answerApi.insert({ ...qa.answer, responseId: responseId! }));
                }

                this.setState({ submissionComplete: true });
            });
        }

        event.preventDefault();
    }

    handleAnswerChange(event: React.ChangeEvent<HTMLInputElement>) {
        const id = Number(event.target.name);
        const value = event.target.value;
        this.setState(prevState => ({
            questionsAndAnswers: prevState.questionsAndAnswers ? prevState.questionsAndAnswers.map(item => item.question.id == id ? { ...item, answer: { ...item.answer, value: value } } : item) : null
        }));
    }

    handleOptionChange(event: React.ChangeEvent<HTMLSelectElement>) {
        const id = Number(event.target.name);
        const optionId: number | null = event.target.value ? Number(event.target.value) : null;
        this.setState(prevState => ({
            questionsAndAnswers: prevState.questionsAndAnswers ? prevState.questionsAndAnswers.map(item => item.question.id == id ? { ...item, answer: { ...item.answer, optionValue: optionId } } : item) : null
        }));
    }

    render() {
        if (this.state.error) {
            return (<ErrorControl message={this.state.message ? this.state.message : "Error Retrieving Survey"} />);
        } else {
            if (this.state.data) {
                const jumboStyle = (this.state.data.bannerImage) ? {
                    backgroundImage: `url(/api/image/${this.state.data.bannerImage})`,
                    color: 'black'
                } : { color: 'black' };

                // color: this.state.data.bannerColor ? this.state.data.bannerColor : 'black'  // foreshadowing... 

                if (this.state.submissionComplete) {
                    return (
                        <div>
                            <div className='jumbotron' style={jumboStyle}>
                                <h1 className='display-4'>{this.state.data.title}</h1>
                                <hr className='my-4' />
                                <p className='lead'>{this.state.data.description}</p>
                            </div>
                            <h1>Thanks for taking the survey.</h1>
                        </div>)
                } else {
                    return (
                        <div>
                            <div className='jumbotron' style={jumboStyle}>
                                <h1 className='display-4'>{this.state.data.title}</h1>
                                <hr className='my-4' />
                                <p className='lead'>{this.state.data.description}</p>
                            </div>
                            <form autoComplete='off' onSubmit={this.handleSubmit}>
                                {this.state.questionsAndAnswers ? this.state.questionsAndAnswers.sort((a, b) => (a.question.displayOrder ? a.question.displayOrder : this.defaultDisplayOrder) - (b.question.displayOrder ? b.question.displayOrder : this.defaultDisplayOrder)).map(qa =>
                                    <div className="form-group">
                                        <label htmlFor={`question${qa.question.id}`}>{qa.question.title}{qa.question.required ? <span className="text-danger">*</span> : null}</label>
                                        {qa.options && qa.options.length > 0 ?
                                            <select name={qa.question.id.toString()} className="form-control" id={`question${qa.question.id}`} value={qa.answer.optionValue ? qa.answer.optionValue : undefined} onChange={this.handleOptionChange} required={qa.question.required ? qa.question.required : undefined} >
                                                <option value=""></option>
                                                {qa.options.map(opt => {
                                                    return <option value={opt.id}>{opt.display}</option>
                                                })}
                                            </select> :
                                            <input name={qa.question.id.toString()} className="form-control" id={`question${qa.question.id}`} type="text" placeholder="enter your answer" value={qa.answer.value ? qa.answer.value : ""} onChange={this.handleAnswerChange} required={qa.question.required ? qa.question.required : undefined} />}
                                        <small className="form-text text-muted">{qa.question.notes}</small>
                                    </div>
                                ) : null}
                                <input type="submit" className="btn btn-primary" value="Complete Survey" />
                            </form>
                        </div>
                    )
                }
            }
            else {
                return (<Loading />);
            }
        }
    }

    async getItemDetails(id: number) {
        await this.api.selectForDisplayById(id).then(data => {
            if (data.parsedBody && data.parsedBody.length > 0) {
                this.setState({ data: data.parsedBody[0], loading: false });
            } else {
                this.setState({ data: null, loading: false, error: true, message: "Item Not Found" });
            }
        }).catch(err => {
            this.setState({ loading: false, error: true, message: err.message });
        });
    }

    async getQuestionDetails(id: any) {
        await this.questionApi.selectForDisplayBySurveyId(id).then(data => {
            var responseBody = data.parsedBody;
            if (responseBody) {
                var qna = this.createQuestionAnswerPairs(responseBody);
                this.setState({ loadingQuestion: false, questionsAndAnswers: qna });
                qna.map(qa => this.getQuestionOptions(qa.question.id));
            }
        }).catch(err => {
            this.setState({ loadingQuestion: false, questionError: true });
        });
    }

    createQuestionAnswerPairs(questions: QuestionDisplay[]): QuestionAnswer[] {
        return questions.map(q => ({ question: q, answer: { id: 0, questionId: q.id, responseId: 0, value: "", optionValue: null }, options: null, loadingOptions: true, loadingOptionsError: false }));
    }

    async getQuestionOptions(questionId: number) {
        await this.questionOptionApi.selectByQuestionId(questionId).then(data => {
            var options = data.parsedBody;
            this.setState(prevState => ({
                questionsAndAnswers: prevState.questionsAndAnswers ? prevState.questionsAndAnswers.map(item => item.question.id == questionId ? { ...item, loadingOptions: false, options: options! } : item) : null
            }));
        }).catch(err => {
            this.setState(prevState => ({
                questionsAndAnswers: prevState.questionsAndAnswers ? prevState.questionsAndAnswers.map(item => item.question.id == questionId ? { ...item, loadingOptions: false, loadingOptionsError: true } : item) : null
            }));
        });
    }
}
